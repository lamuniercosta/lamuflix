using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Logging;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class BackfillTmdbRecommendationsCommandHandler(
    IMovieRepository movies,
    IMetadataProvider provider,
    ILogger<BackfillTmdbRecommendationsCommandHandler> logger)
    : ICommandHandler<BackfillTmdbRecommendationsCommand, BackfillTmdbRecommendationsResult>
{
    public async Task<BackfillTmdbRecommendationsResult> HandleAsync(
        BackfillTmdbRecommendationsCommand command,
        CancellationToken cancellationToken)
    {
        var counts = new BackfillCounts();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ids = await movies.SnapshotMovieIdsAsync(cancellationToken);
            foreach (var id in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await BackfillAsync(id, counts, cancellationToken) is { } stopReason)
                {
                    return counts.Result(stopReason);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return counts.Result(BackfillStopReason.Cancelled);
        }

        return counts.Result(stopReason: null);
    }

    private async Task<BackfillStopReason?> BackfillAsync(
        MovieId id,
        BackfillCounts counts,
        CancellationToken cancellationToken)
    {
        var movie = await movies.GetAsync(id, cancellationToken);
        if (movie is null || ImdbIdOf(movie) is not { } imdbId)
        {
            counts.Skipped++;
            return null;
        }

        counts.ImdbEligible++;
        if (movie.TmdbId is not null && !movie.HasUntitledRecommendations)
        {
            counts.Skipped++;
            return null;
        }

        var result = await FindAsync(movie, imdbId, cancellationToken);
        return await SettleAsync(movie, result, counts, cancellationToken);
    }

    private async Task<MetadataLookupResult> FindAsync(Movie movie, ImdbId imdbId, CancellationToken cancellationToken)
    {
        try
        {
            var lookup = new MetadataLookup(
                movie.Title,
                movie.Metadata?.ReleaseYear ?? movie.ParsedReleaseYear,
                imdbId);
            return await provider.FindAsync(lookup, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new MetadataLookupResult.Failed(EnrichmentFailureClassifier.Classify(exception));
        }
    }

    private async Task<BackfillStopReason?> SettleAsync(
        Movie movie,
        MetadataLookupResult result,
        BackfillCounts counts,
        CancellationToken cancellationToken)
    {
        if (result is MetadataLookupResult.NotFound)
        {
            counts.Unmatched++;
            Log(movie.Id, "unmatched", category: null);
            return null;
        }

        if (result is MetadataLookupResult.Failed failed)
        {
            counts.Failures++;
            Log(movie.Id, "failed", failed.Category);
            return ReferenceEquals(failed.Category, EnrichmentFailureCategory.ProviderUnavailable)
                ? BackfillStopReason.ProviderUnavailable
                : null;
        }

        return await SettleMatchAsync(movie, result as MetadataLookupResult.Found, counts, cancellationToken);
    }

    private async Task<BackfillStopReason?> SettleMatchAsync(
        Movie movie,
        MetadataLookupResult.Found? found,
        BackfillCounts counts,
        CancellationToken cancellationToken)
    {
        if (found?.Metadata.TmdbId is not { } tmdbId || found.Metadata.RecommendedTmdbMovies is not { } recommended)
        {
            counts.Failures++;
            Log(movie.Id, "failed", EnrichmentFailureCategory.InvalidResponse);
            return null;
        }

        movie.RecordBackfilledTmdbIdentity(tmdbId, recommended);
        await movies.SaveChangesAsync(cancellationToken);
        counts.Matched++;
        Log(movie.Id, "matched", category: null);
        return null;
    }

    private static ImdbId? ImdbIdOf(Movie movie) => movie.Metadata?.ImdbId ?? movie.ManualImdbId;

    private void Log(MovieId id, string outcome, EnrichmentFailureCategory? category) =>
        logger.LogInformation(
            "TMDb backfill {Outcome} for {MovieId}{CategorySuffix}",
            outcome,
            id.Value,
            category is null ? string.Empty : $" category {category.Code}");

    private sealed class BackfillCounts
    {
        public int ImdbEligible { get; set; }

        public int Matched { get; set; }

        public int Unmatched { get; set; }

        public int Skipped { get; set; }

        public int Failures { get; set; }

        public BackfillTmdbRecommendationsResult Result(BackfillStopReason? stopReason) =>
            new(ImdbEligible, Matched, Unmatched, Skipped, Failures, stopReason);
    }
}
