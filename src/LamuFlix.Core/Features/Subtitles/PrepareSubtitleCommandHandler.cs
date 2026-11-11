using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Subtitles;

public sealed class PrepareSubtitleCommandHandler(
    IMovieCatalog catalog,
    IMediaLibraryScanner scanner,
    ILibraryFileReader reader,
    ISubtitleSidecarStore store,
    ISubtitleProvider provider,
    IOptions<SubtitlesOptions> subtitlesOptions,
    IOptions<SubtitleProviderOptions> providerOptions,
    ILogger<PrepareSubtitleCommandHandler> logger)
    : ICommandHandler<PrepareSubtitleCommand, SubtitlePreparationResult>
{
    public async Task<SubtitlePreparationResult> HandleAsync(
        PrepareSubtitleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        SubtitleDiagnostics.TagMovie(command.Id);
        using var activity = SubtitleTelemetry.StartPrepare();
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await PrepareAsync(command, cancellationToken);
            SubtitleTelemetry.Complete(activity, command.Id, result.Outcome.Code, Stopwatch.GetElapsedTime(started));
            LogOutcome(command.Id, result);
            return result;
        }
        catch (Exception exception)
        {
            SubtitleDiagnostics.TagFailure(exception);
            SubtitleTelemetry.Fail(activity, command.Id, exception);
            throw;
        }
    }

    private async Task<SubtitlePreparationResult> PrepareAsync(
        PrepareSubtitleCommand command,
        CancellationToken cancellationToken)
    {
        var target = await SubtitlePreparationTarget.ResolveAsync(
            catalog, scanner, reader, command.Id, cancellationToken);

        if (await store.ExistsAsync(target.Folder, target.PrimaryFileName, cancellationToken))
        {
            return SubtitlePreparationResult.AlreadyPresent();
        }

        var settings = SubtitleAcquisitionSettings.Require(subtitlesOptions, providerOptions);
        var hash = await MovieHashReader.ReadAsync(reader, target.PrimaryFile, cancellationToken);

        var candidate = await SubtitleLookup.FindAsync(
            provider,
            new SubtitleLookupRequest(hash.MovieHash, hash.FileLength, target.Title, settings.PreferredLanguages),
            cancellationToken);

        if (candidate is null)
        {
            return SubtitlePreparationResult.NoMatch();
        }

        var content = await provider.DownloadAsync(candidate, cancellationToken);
        var write = await store.CreateAsync(target.Folder, target.PrimaryFileName, content, cancellationToken);
        return write == SubtitleSidecarWriteResult.Created
            ? SubtitlePreparationResult.Saved(candidate.Language)
            : SubtitlePreparationResult.AlreadyPresent();
    }

    private void LogOutcome(LamuFlix.Core.Domain.MovieId movieId, SubtitlePreparationResult result) =>
        logger.LogInformation(
            "Subtitle preparation for {MovieId} finished with {Outcome}.",
            movieId.Value,
            result.Outcome.Code);
}
