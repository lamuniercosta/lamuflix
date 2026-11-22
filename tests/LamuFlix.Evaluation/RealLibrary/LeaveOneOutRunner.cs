using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Evaluation.Metrics;

namespace LamuFlix.Evaluation.RealLibrary;

public static class LeaveOneOutRunner
{
    private const string BypassDisclosure =
        "The HTTP recommendation endpoint and its request validator are bypassed; the real For-you "
        + "handler is invoked directly with take equal to the eligible candidate count.";

    private const string HarnessRelativeDisclosure =
        "Absolute HR@1/3/10 and MRR are harness-relative full-eligible-ranking measurements; only "
        + "same-protocol deltas are comparable across runs.";

    public static async Task<LeaveOneOutResult> RunAsync(
        RealLibrarySnapshot snapshot,
        RecommendationOptions options,
        TimeProvider timeProvider,
        CancellationToken ct,
        TmdbOptions? tmdbOptions = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var cohorts = Cohorts(snapshot);
        var liked = await RankCohortAsync(snapshot, options, timeProvider, tmdbOptions, cohorts.Liked, ct);
        var watched = await RankCohortAsync(snapshot, options, timeProvider, tmdbOptions, cohorts.Watched, ct);

        return new LeaveOneOutResult(
            liked,
            watched,
            [BypassDisclosure, HarnessRelativeDisclosure],
            new CohortOverlap(
                LikedCount: cohorts.Liked.Length,
                WatchedCount: cohorts.Watched.Length,
                OverlapCount: cohorts.Liked.Count(id => cohorts.Watched.Contains(id))));
    }

    private static async Task<CohortResult> RankCohortAsync(
        RealLibrarySnapshot snapshot,
        RecommendationOptions options,
        TimeProvider timeProvider,
        TmdbOptions? tmdbOptions,
        ImmutableArray<MovieId> seeds,
        CancellationToken ct)
    {
        var observations = new List<EvaluationObservation>(seeds.Length);
        var attempts = new List<LeaveOneOutAttempt>(seeds.Length);

        foreach (var seed in seeds)
        {
            ct.ThrowIfCancellationRequested();

            var copy = HiddenSeedEvaluationCopy.Create(snapshot, seed, options, timeProvider, tmdbOptions);
            var take = EligibleCandidateCount(snapshot, seed);
            var ranking = await copy.RankAsync(take, ct);

            var ranks = ImmutableArray.CreateRange(ranking.Select(movie => movie.Summary.Id));
            observations.Add(new EvaluationObservation(seed, ranks));
            attempts.Add(new LeaveOneOutAttempt(seed, take, ranks, RankOf(seed, ranks)));
        }

        return new CohortResult(
            [.. attempts],
            RealLibraryMetricCalculator.Compute(observations, MetricCalculator.Cutoff));
    }

    private static int RankOf(MovieId seed, ImmutableArray<MovieId> ranking)
    {
        var index = ranking.IndexOf(seed);

        return index < 0 ? 0 : index + 1;
    }

    private static int EligibleCandidateCount(RealLibrarySnapshot snapshot, MovieId seed) =>
        snapshot.Movies.Count(movie => movie.Id == seed || (!movie.IsWatched && !movie.Review.IsDisliked));

    private static CohortSeeds Cohorts(RealLibrarySnapshot snapshot)
    {
        var liked = ImmutableArray.CreateBuilder<MovieId>();
        var watched = ImmutableArray.CreateBuilder<MovieId>();

        foreach (var movie in snapshot.Movies)
        {
            if (movie.Review.IsLiked)
            {
                liked.Add(movie.Id);
            }

            if (movie.IsWatched)
            {
                watched.Add(movie.Id);
            }
        }

        return new CohortSeeds([.. liked], [.. watched]);
    }

    private sealed record CohortSeeds(ImmutableArray<MovieId> Liked, ImmutableArray<MovieId> Watched);
}

public sealed record LeaveOneOutAttempt(
    MovieId Seed,
    int Take,
    ImmutableArray<MovieId> Ranking,
    int Rank);

public sealed record CohortResult(
    ImmutableArray<LeaveOneOutAttempt> Attempts,
    RealLibraryMetrics Metrics);

public sealed record CohortOverlap(int LikedCount, int WatchedCount, int OverlapCount);

public sealed record LeaveOneOutResult(
    CohortResult Liked,
    CohortResult Watched,
    ImmutableArray<string> Disclosures,
    CohortOverlap Overlap);
