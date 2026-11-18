using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.Evaluation.RealLibrary;

public sealed class LeaveOneOutRunnerTests
{
    private static readonly RecommendationOptions Options = new();

    [Fact]
    public async Task RunAsync_ranks_every_seed_and_reports_full_ranking_metrics()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Alpha", review: Review.Liked, genres: ["Drama"]),
                BuildMovie(2, "Bravo", genres: ["Drama"]),
                BuildMovie(3, "Charlie", genres: ["Comedy"]),
                BuildMovie(4, "Delta", watched: true, genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(1.0)), (3, Vector(-1.0)), (4, Vector(1.0))));

        // act
        var result = await LeaveOneOutRunner.RunAsync(snapshot, Options, TimeProvider.System, CancellationToken.None);

        // assert
        var attempt = result.Liked.Attempts.ShouldHaveSingleItem();
        attempt.Seed.ShouldBe(new MovieId(1));
        attempt.Take.ShouldBe(3);
        attempt.Ranking.ShouldBe([new MovieId(1), new MovieId(2), new MovieId(3)]);
        attempt.Rank.ShouldBe(1);

        result.Liked.Metrics.AttemptedCount.ShouldBe(1);
        result.Liked.Metrics.HitRateAt1.ShouldBe(1.0);
        result.Liked.Metrics.HitRateAt3.ShouldBe(1.0);
        result.Liked.Metrics.HitRateAt10.ShouldBe(1.0);
        result.Liked.Metrics.Mrr.ShouldBe(1.0);
    }

    [Fact]
    public async Task RunAsync_absent_target_contributes_zero_rather_than_shrinking_the_denominator()
    {
        // arrange — the only positive signal is the liked seed itself, so hiding it leaves no profile
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Alpha", review: Review.Liked, genres: ["Drama"]),
                BuildMovie(2, "Bravo", genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(1.0))));

        // act
        var result = await LeaveOneOutRunner.RunAsync(snapshot, Options, TimeProvider.System, CancellationToken.None);

        // assert
        var attempt = result.Liked.Attempts.ShouldHaveSingleItem();
        attempt.Ranking.ShouldBeEmpty();
        attempt.Rank.ShouldBe(0);

        result.Liked.Metrics.AttemptedCount.ShouldBe(1);
        result.Liked.Metrics.HitRateAt1.ShouldBe(0.0);
        result.Liked.Metrics.HitRateAt10.ShouldBe(0.0);
        result.Liked.Metrics.Mrr.ShouldBe(0.0);
    }

    [Fact]
    public async Task RunAsync_zero_seed_cohort_reports_null_unavailable_metrics()
    {
        // arrange — no liked and no watched signal anywhere
        var snapshot = Snapshot(
            Movies(BuildMovie(1, "Alpha", genres: ["Drama"]), BuildMovie(2, "Bravo", genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(1.0))));

        // act
        var result = await LeaveOneOutRunner.RunAsync(snapshot, Options, TimeProvider.System, CancellationToken.None);

        // assert
        result.Liked.Attempts.ShouldBeEmpty();
        result.Watched.Attempts.ShouldBeEmpty();
        result.Liked.Metrics.ShouldBe(new RealLibraryMetrics(0, null, null, null, null));
        result.Watched.Metrics.ShouldBe(new RealLibraryMetrics(0, null, null, null, null));
        result.Overlap.ShouldBe(new CohortOverlap(0, 0, 0));
    }

    [Fact]
    public async Task RunAsync_retains_and_discloses_liked_and_watched_overlap()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Alpha", watched: true, review: Review.Liked, genres: ["Drama"]),
                BuildMovie(2, "Bravo", genres: ["Drama"]),
                BuildMovie(3, "Charlie", watched: true, genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(1.0)), (3, Vector(1.0))));

        // act
        var result = await LeaveOneOutRunner.RunAsync(snapshot, Options, TimeProvider.System, CancellationToken.None);

        // assert
        result.Liked.Attempts.Select(attempt => attempt.Seed).ShouldBe([new MovieId(1)]);
        result.Watched.Attempts.Select(attempt => attempt.Seed).ShouldBe([new MovieId(1), new MovieId(3)]);
        result.Overlap.ShouldBe(new CohortOverlap(LikedCount: 1, WatchedCount: 2, OverlapCount: 1));
        result.Liked.Metrics.AttemptedCount.ShouldBe(1);
        result.Watched.Metrics.AttemptedCount.ShouldBe(2);
    }

    [Fact]
    public async Task RunAsync_discloses_endpoint_bypass_and_harness_relative_validity()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(BuildMovie(1, "Alpha", review: Review.Liked, genres: ["Drama"]), BuildMovie(2, "Bravo", genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(1.0))));

        // act
        var result = await LeaveOneOutRunner.RunAsync(snapshot, Options, TimeProvider.System, CancellationToken.None);

        // assert
        result.Disclosures.ShouldContain(disclosure => disclosure.Contains("validator", StringComparison.Ordinal));
        result.Disclosures.ShouldContain(disclosure => disclosure.Contains("harness-relative", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_scoring_failure_fails_the_run_instead_of_shrinking_the_cohort()
    {
        // arrange — a profile embedding with the wrong dimension is a scoring input failure
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Alpha", review: Review.Liked, genres: ["Drama"]),
                BuildMovie(2, "Bravo", genres: ["Drama"]),
                BuildMovie(3, "Charlie", watched: true, genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(1.0)), (3, Vector(1.0))),
            profileOverrides: (3, [1f, 2f, 3f]));

        // act
        var act = () => LeaveOneOutRunner.RunAsync(snapshot, Options, TimeProvider.System, CancellationToken.None);

        // assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    private static RealLibrarySnapshot Snapshot(
        ImmutableArray<Movie> movies,
        ImmutableDictionary<MovieId, ImmutableArray<float>> vectors,
        (int Id, float[] Embedding)? profileOverrides = null)
    {
        var profileMovies = ImmutableArray.CreateBuilder<TasteProfileMovie>();

        foreach (var movie in movies)
        {
            if (!movie.IsWatched && movie.Review.IsNone)
            {
                continue;
            }

            var embedding = profileOverrides is { } overrides && overrides.Id == movie.Id.Value
                ? [.. overrides.Embedding]
                : vectors.GetValueOrDefault(movie.Id, ImmutableArray<float>.Empty);

            profileMovies.Add(new TasteProfileMovie(
                movie.Id,
                Genres(movie),
                embedding,
                ImmutableArray<DateTimeOffset>.Empty,
                movie.IsWatched)
            {
                Review = movie.Review,
                Title = movie.Title,
            });
        }

        var genres = movies
            .SelectMany(movie => Genres(movie))
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(genre => genre, _ => (long)movies.Length, StringComparer.OrdinalIgnoreCase);

        return new RealLibrarySnapshot(
            movies,
            profileMovies.ToImmutable(),
            vectors,
            new GenreCounts(movies.Length, genres));
    }

    private static ImmutableArray<string> Genres(Movie movie) => movie.Metadata?.Genres ?? [];

    private static ImmutableArray<Movie> Movies(params Movie[] movies) => [.. movies];

    private static ImmutableDictionary<MovieId, ImmutableArray<float>> Vectors(
        params (int Id, float[] Vector)[] vectors) =>
        vectors.ToImmutableDictionary(
            entry => new MovieId(entry.Id),
            entry => ImmutableArray.CreateRange(entry.Vector));

    private static Movie BuildMovie(
        int id,
        string title,
        bool watched = false,
        Review? review = null,
        ImmutableArray<string> genres = default) =>
        Movie.Rehydrate(
            new MovieId(id),
            title,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            isInWatchlist: false,
            metadata: new MovieMetadata(
                title,
                genres: genres.IsDefault ? ["Drama"] : genres),
            status: EnrichmentStatus.Enriched,
            enrichedAt: null,
            enrichmentAttempts: 0,
            lastFailureCategory: null,
            lastAttemptAt: null,
            isWatched: watched,
            review: review ?? Review.None);

    private static float[] Vector(double cosine)
    {
        var values = new float[EmbeddingDimensions.Value];
        values[0] = (float)cosine;
        values[1] = (float)Math.Sqrt(Math.Max(0.0, 1.0 - (cosine * cosine)));
        return values;
    }
}
