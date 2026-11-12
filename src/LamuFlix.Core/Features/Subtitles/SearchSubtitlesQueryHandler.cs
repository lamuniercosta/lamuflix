using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Subtitles;

public sealed class SearchSubtitlesQueryHandler(
    IMovieCatalog catalog,
    IMediaLibraryScanner scanner,
    ILibraryFileReader reader,
    ISubtitleProvider provider,
    SubtitleCandidateBinder binder,
    IOptions<SubtitlesOptions> subtitlesOptions,
    IOptions<SubtitleProviderOptions> providerOptions)
    : IQueryHandler<SearchSubtitlesQuery, SearchSubtitlesResult>
{
    public async Task<SearchSubtitlesResult> HandleAsync(
        SearchSubtitlesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        SubtitleDiagnostics.TagMovie(query.Id);
        using var activity = SubtitleTelemetry.StartProviderLookup();
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await SearchAsync(query, cancellationToken);
            SubtitleTelemetry.Complete(
                activity,
                query.Id,
                TelemetryConstants.SucceededOutcome,
                Stopwatch.GetElapsedTime(started));
            return result;
        }
        catch (Exception exception)
        {
            SubtitleDiagnostics.TagFailure(exception);
            SubtitleTelemetry.Fail(activity, query.Id, exception);
            throw;
        }
    }

    private async Task<SearchSubtitlesResult> SearchAsync(
        SearchSubtitlesQuery query,
        CancellationToken cancellationToken)
    {
        var language = ResolveLanguage(query.Language);
        var target = await ResolveTargetAsync(query.Id, cancellationToken);
        EnsureProviderConfigured();

        var search = await SearchProviderAsync(target, language.Code, cancellationToken);
        var candidates = Rank(query.Id, language.Code, search.Candidates, VideoStem(target.PrimaryFileName));
        var downloadEnabled = subtitlesOptions.Value.SaveEnabled;

        return new SearchSubtitlesResult(
            candidates,
            search.Quota,
            downloadEnabled,
            downloadEnabled ? string.Empty : SubtitleProblemReasons.ReadOnlyLibrary);
    }

    private static string VideoStem(string primaryFileName) => Path.GetFileNameWithoutExtension(primaryFileName);

    private static SubtitleLanguage ResolveLanguage(string? requested) =>
        SupportedSubtitleLanguages.TryResolve(requested, out var language)
            ? language
            : throw new SubtitleRequestRejectedException(
                "The requested subtitle language is not a supported language code.");

    private async Task<SubtitlePreparationTarget> ResolveTargetAsync(
        MovieId movieId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await SubtitlePreparationTarget.ResolveAsync(catalog, scanner, reader, movieId, cancellationToken);
        }
        catch (NotFoundException exception)
        {
            throw new SubtitleMovieNotFoundException("The movie is not present in the library.", exception);
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

    private async Task<SubtitleSearchResult> SearchProviderAsync(
        SubtitlePreparationTarget target,
        string languageCode,
        CancellationToken cancellationToken)
    {
        var hash = await MovieHashReader.ReadAsync(reader, target.PrimaryFile, cancellationToken);
        if (!string.IsNullOrWhiteSpace(hash.MovieHash))
        {
            var hashResult = await SendSearchAsync(
                new SubtitleSearch(hash.MovieHash, hash.FileLength, Title: null, [languageCode]),
                cancellationToken);
            if (hashResult.Candidates.Count > 0)
            {
                return hashResult;
            }
        }

        if (string.IsNullOrWhiteSpace(target.Title))
        {
            return new SubtitleSearchResult([], SubtitleQuota.Unknown);
        }

        return await SendSearchAsync(
            new SubtitleSearch(MovieHash: null, hash.FileLength, target.Title, [languageCode]),
            cancellationToken);
    }

    private async Task<SubtitleSearchResult> SendSearchAsync(
        SubtitleSearch search,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.SearchAsync(search, cancellationToken);
        }
        catch (SubtitleQuotaExceededException exception)
        {
            throw new SubtitleSearchRateLimitedException(exception.RetryAfter);
        }
    }

    private ImmutableArray<SubtitleSearchCandidate> Rank(
        MovieId movieId,
        string languageCode,
        IReadOnlyList<SubtitleCandidate> candidates,
        string videoStem)
    {
        var entries = new List<RankedCandidate>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var releaseName = candidate.ReleaseName ?? string.Empty;
            entries.Add(new RankedCandidate(
                binder.Issue(movieId, languageCode, candidate, releaseName),
                candidate,
                releaseName,
                SubtitleMatchScorer.Score(videoStem, releaseName, candidate.MatchesMovieHash)));
        }

        entries.Sort(CompareRanked);
        return [.. entries.Select(entry => new SubtitleSearchCandidate(
            entry.CandidateId,
            languageCode,
            entry.ReleaseName,
            entry.MatchPercent,
            entry.Candidate.DownloadCount))];
    }

    private static int CompareRanked(RankedCandidate first, RankedCandidate second)
    {
        if (first.MatchPercent != second.MatchPercent)
        {
            return second.MatchPercent.CompareTo(first.MatchPercent);
        }

        if (first.Candidate.DownloadCount != second.Candidate.DownloadCount)
        {
            return second.Candidate.DownloadCount.CompareTo(first.Candidate.DownloadCount);
        }

        return SubtitleMatchScorer.Compare(
            first.ReleaseName,
            first.CandidateId,
            second.ReleaseName,
            second.CandidateId);
    }

    private readonly record struct RankedCandidate(
        string CandidateId,
        SubtitleCandidate Candidate,
        string ReleaseName,
        int MatchPercent);
}
