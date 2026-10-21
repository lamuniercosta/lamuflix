using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GetRecommendationsQueryHandlerPropertyTests
{
    private const int SelectedMovieId = 1;
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
            ["Horror"] = 5,
        });

    [Property(MaxTest = 100, Replay = "340101,100003")]
    [Trait("Category", "Property")]
    public bool HandleAsync_GeneratedPool_OrdersByScoreDescendingThenIdAscending(int[] ids, int[] seeds, int takeSeed)
    {
        // arrange
        var pool = BuildPool(ids, seeds);
        var take = Take(takeSeed);

        // act
        var result = Handle(pool, take);

        // assert
        return IsOrdered(result);
    }

    [Property(MaxTest = 100, Replay = "340102,200009")]
    [Trait("Category", "Property")]
    public bool HandleAsync_GeneratedPool_CountIsMinOfTakeAndPoolSize(int[] ids, int[] seeds, int takeSeed)
    {
        // arrange
        var pool = BuildPool(ids, seeds);
        var take = Take(takeSeed);

        // act
        var result = Handle(pool, take);

        // assert
        return result.Count == Math.Min(take, pool.Length);
    }

    [Property(MaxTest = 100, Replay = "340103,300011")]
    [Trait("Category", "Property")]
    public bool HandleAsync_PermutedPool_ReturnsIdenticalOrdering(int[] ids, int[] seeds, int takeSeed)
    {
        // arrange
        var pool = BuildPool(ids, seeds);
        var take = Take(takeSeed);

        // act
        var forward = Handle(pool, take);
        var reversed = Handle([.. pool.Reverse()], take);
        var sorted = Handle([.. pool.OrderByDescending(movie => movie.Id.Value)], take);

        // assert
        return SameSequence(forward, reversed) && SameSequence(forward, sorted);
    }

    private static ImmutableList<RecommendedMovie> Handle(IReadOnlyList<Movie> pool, int take)
    {
        var movies = Substitute.For<IMovieRepository>();
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(SelectedMovie());
        var source = Substitute.For<IRecommendationCandidateSource>();
        source.GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(pool);
        var catalog = Substitute.For<IMovieCatalog>();
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Counts);
        var handler = new GetRecommendationsQueryHandler(
            movies,
            source,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            Microsoft.Extensions.Options.Options.Create(new RecommendationOptions()));

        return handler
            .HandleAsync(new GetRecommendationsQuery(new MovieId(SelectedMovieId), take), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    private static int Take(int takeSeed) => Math.Abs(takeSeed % 20) + 1;

    private static Movie[] BuildPool(int[] ids, int[] seeds)
    {
        var distinct = ids
            .Select(value => Math.Abs(value % 900) + 2)
            .Distinct()
            .Take(12)
            .ToArray();

        var pool = new Movie[distinct.Length];
        for (var index = 0; index < distinct.Length; index++)
        {
            var seed = seeds.Length == 0 ? distinct[index] : seeds[index % seeds.Length];
            pool[index] = BuildMovie(distinct[index], seed);
        }

        return pool;
    }

    private static Movie BuildMovie(int id, int seed)
    {
        var magnitude = Math.Abs((long)seed);
        var title = $"Movie {id}";

        if (magnitude % 6 == 0)
        {
            return Movie.Rehydrate(
                new MovieId(id),
                title,
                new LibraryPath($"C:/library/{id}.mkv"),
                new MediaFormat("mkv"),
                false,
                null,
                EnrichmentStatus.Enriched,
                Now,
                1,
                null,
                null);
        }

        var metadata = new MovieMetadata(
            title,
            posterUrl: $"https://posters/{id}",
            releaseYear: new ReleaseYear(1990 + (int)(magnitude % 30), Now),
            runtime: new Runtime(80 + (int)(magnitude % 80)),
            imdbRating: new ImdbRating((magnitude % 101) / 10.0m),
            directors: [(magnitude & 1) == 0 ? "Director A" : "Director B"],
            actors: [(magnitude & 2) == 0 ? "Actor A" : "Actor B"],
            genres: Genres(magnitude),
            writers: [(magnitude & 4) == 0 ? "Writer A" : "Writer B"]);

        return Movie.Rehydrate(
            new MovieId(id),
            title,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            false,
            metadata,
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null);
    }

    private static string[] Genres(long magnitude)
    {
        var palette = new[] { "Drama", "Comedy", "Horror", "Action" };
        return [.. palette.Where((_, index) => ((magnitude >> index) & 1) == 1)];
    }

    private static Movie SelectedMovie() => Movie.Rehydrate(
        new MovieId(SelectedMovieId),
        "Selected",
        new LibraryPath("C:/library/selected.mkv"),
        new MediaFormat("mkv"),
        false,
        new MovieMetadata(
            "Selected",
            releaseYear: new ReleaseYear(2000, Now),
            runtime: new Runtime(120),
            imdbRating: new ImdbRating(8.0m),
            directors: ["Director A"],
            actors: ["Actor A"],
            genres: ["Drama", "Action"],
            writers: ["Writer A"]),
        EnrichmentStatus.Enriched,
        Now,
        1,
        null,
        null);

    private static bool IsOrdered(ImmutableList<RecommendedMovie> result)
    {
        for (var index = 1; index < result.Count; index++)
        {
            var previous = result[index - 1];
            var current = result[index];

            if (previous.Score < current.Score)
            {
                return false;
            }

            if (previous.Score == current.Score && previous.Summary.Id.Value > current.Summary.Id.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameSequence(ImmutableList<RecommendedMovie> first, ImmutableList<RecommendedMovie> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (var index = 0; index < first.Count; index++)
        {
            if (!SameRecommendation(first[index], second[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameRecommendation(RecommendedMovie first, RecommendedMovie second) =>
        first.Summary == second.Summary
        && first.Score == second.Score
        && first.Reasons.Select(reason => reason.Text)
            .SequenceEqual(second.Reasons.Select(reason => reason.Text), StringComparer.Ordinal);
}
