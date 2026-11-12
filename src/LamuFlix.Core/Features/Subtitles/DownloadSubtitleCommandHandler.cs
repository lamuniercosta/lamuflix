using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Subtitles;

public sealed class DownloadSubtitleCommandHandler(
    IMovieCatalog catalog,
    IMediaLibraryScanner scanner,
    ILibraryFileReader reader,
    ISubtitleSidecarStore store,
    ISubtitleProvider provider,
    SubtitleCandidateBinder binder,
    IOptions<SubtitlesOptions> subtitlesOptions,
    IOptions<SubtitleProviderOptions> providerOptions,
    IOptions<LibraryOptions> libraryOptions,
    ILogger<DownloadSubtitleCommandHandler> logger)
    : ICommandHandler<DownloadSubtitleCommand, DownloadSubtitleResult>
{
    private const string SubsFilePrefix = "Subs/";

    public async Task<DownloadSubtitleResult> HandleAsync(
        DownloadSubtitleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        SubtitleDiagnostics.TagMovie(command.Id);
        using var activity = SubtitleTelemetry.StartDownload();
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await DownloadAsync(command, cancellationToken);
            SubtitleTelemetry.Complete(
                activity,
                command.Id,
                TelemetryConstants.SucceededOutcome,
                Stopwatch.GetElapsedTime(started));
            logger.LogInformation(
                "Subtitle download for {MovieId} published a {Language} track.",
                command.Id.Value,
                result.Track.Language);
            return result;
        }
        catch (Exception exception)
        {
            SubtitleDiagnostics.TagFailure(exception);
            SubtitleTelemetry.Fail(activity, command.Id, exception);
            throw;
        }
    }

    private async Task<DownloadSubtitleResult> DownloadAsync(
        DownloadSubtitleCommand command,
        CancellationToken cancellationToken)
    {
        var details = await catalog.GetDetailsAsync(command.Id, cancellationToken)
            ?? throw new SubtitleMovieNotFoundException("The movie is not present in the library.");

        var binding = ResolveBinding(command);
        var settings = subtitlesOptions.Value;
        EnsureWritable(settings);
        EnsureProviderConfigured();

        reader.AuthorizeFolder(details.Path);
        var target = ResolveTarget(details);
        await store.EnsurePublishableAsync(target.Folder, target.PrimaryFileName, cancellationToken);

        if (binding.LastKnownQuota.IsExhausted)
        {
            throw new SubtitleDownloadQuotaExhaustedException(binding.LastKnownQuota);
        }

        var download = await provider.DownloadAsync(binding.Candidate, cancellationToken);
        if (download.Content.Length == 0 || download.Content.Length > settings.MaxSubtitleDownloadBytes)
        {
            throw new SubtitleProviderFailureException(
                "The downloaded subtitle exceeds the configured size bound.");
        }

        EnsureValidSrt(download.Content, libraryOptions.Value);

        var published = await store.PublishToSubsAsync(
            target.Folder,
            target.PrimaryFileName,
            binding.Language,
            download.Content,
            settings.MaxSubtitlePublicationAttempts,
            cancellationToken);

        if (published.Outcome is not SubtitlePublishOutcome.Published)
        {
            throw new SubtitleReadOnlyLibraryException(
                "The subtitle could not be published to the Subs folder within the configured attempt ceiling.");
        }

        return new DownloadSubtitleResult(
            BuildTrack(command.Id, target, binding.Language, published.FileName),
            download.Quota);
    }

    private SubtitlePreparationTarget ResolveTarget(MovieDetails details)
    {
        PrimaryVideo primary;
        try
        {
            primary = scanner.ResolvePrimaryVideo(details.Path);
        }
        catch (InvalidTransitionException exception)
        {
            throw new SubtitleMovieNotFoundException("The movie has no primary video file.", exception);
        }

        return new SubtitlePreparationTarget(
            details.Path,
            primary.Path,
            Path.GetFileName(primary.Path.Value),
            details.Title);
    }

    private SubtitleCandidateBinding ResolveBinding(DownloadSubtitleCommand command) =>
        binder.TryResolve(command.CandidateId, command.Id, out var binding)
            ? binding
            : throw new SubtitleStaleCandidateException(
                "The selected subtitle candidate expired or does not belong to this movie.");

    private static void EnsureWritable(SubtitlesOptions settings)
    {
        if (!settings.SaveEnabled)
        {
            throw new SubtitleReadOnlyLibraryException(
                "Subtitle downloads are disabled for this library.");
        }
    }

    private void EnsureProviderConfigured()
    {
        try
        {
            _ = providerOptions.Value;
        }
        catch (OptionsValidationException exception)
        {
            throw new SubtitleProviderNotConfiguredException(
                "The subtitle provider acquisition settings are missing or invalid.",
                exception);
        }
    }

    private static void EnsureValidSrt(byte[] payload, LibraryOptions libraryOptions)
    {
        try
        {
            _ = SubtitleWebVttConverter.Convert(
                SubtitleTextDecoder.Decode(payload),
                new SubtitleConversionLimits(
                    libraryOptions.MaxSubtitleCues,
                    libraryOptions.MaxSubtitleLineCharacters,
                    libraryOptions.MaxSubtitleOutputBytes));
        }
        catch (ValidationException exception)
        {
            throw new SubtitleProviderFailureException(
                "The downloaded subtitle is not a valid SRT document.",
                exception);
        }
    }

    private SubtitleTrack BuildTrack(
        MovieId movieId,
        SubtitlePreparationTarget target,
        string languageCode,
        string publishedFileName)
    {
        var relativeFileName = string.Concat(SubsFilePrefix, publishedFileName);
        var token = SubtitleTrackToken.For(movieId, relativeFileName);

        foreach (var candidate in SubtitleSidecarDiscovery.Discover(
                     target.PrimaryFileName,
                     reader.EnumerateFiles(target.Folder)))
        {
            if (string.Equals(candidate.FileName, relativeFileName, StringComparison.Ordinal))
            {
                return new SubtitleTrack(token, candidate.Label, candidate.Language, candidate.IsDefault);
            }
        }

        var label = SupportedSubtitleLanguages.TryResolve(languageCode, out var language)
            ? language.EnglishName
            : languageCode;
        return new SubtitleTrack(token, label, languageCode, IsDefault: false);
    }
}
