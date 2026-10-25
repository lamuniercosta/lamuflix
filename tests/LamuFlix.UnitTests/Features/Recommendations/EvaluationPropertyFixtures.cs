using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Evaluation.Fixtures;
using LamuFlix.Evaluation.Metrics;

namespace LamuFlix.UnitTests.Features.Recommendations;

/// <summary>
/// Deterministic fixture/observation builders for the DEV-356 evaluation
/// property tests. Every member maps arbitrary FsCheck seeds onto a valid
/// evaluation input so the invariants are exercised over a wide shape range
/// without relying on a shared random state.
/// </summary>
internal static class EvaluationPropertyFixtures
{
    public const int MaxEvents = 8;
    public const int MaxCatalogSize = 6;
    public const int MinCatalogSize = 3;

    private const int MaxObservationCount = 12;
    private const int MaxRankingLength = 12;
    private const int MaxMovieId = 20;

    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly string[] GenreTokens = ["Drama", "Comedy", "Noir", "Sci-Fi"];

    public static EvaluationFixture Fixture(int catalogSeed, int[] eventSeeds, int lambdaSeed)
    {
        var catalog = Catalog(catalogSeed);
        var events = Events(eventSeeds, catalog.Length);
        var inputs = new EvaluationRankingInputs(Lambda(lambdaSeed), new SimilarityWeights());

        return new EvaluationFixture("property-fixture", catalog, inputs, events);
    }

    public static ImmutableArray<EvaluationCatalogMovie> Catalog(int seed)
    {
        var size = MinCatalogSize + (int)((uint)seed % (MaxCatalogSize - MinCatalogSize + 1));
        var builder = ImmutableArray.CreateBuilder<EvaluationCatalogMovie>(size);

        for (var index = 0; index < size; index++)
        {
            builder.Add(new EvaluationCatalogMovie(new MovieId(index + 1), Features(seed + index)));
        }

        return builder.ToImmutable();
    }

    public static ImmutableArray<EvaluationEvent> Events(int[] seeds, int catalogSize)
    {
        var count = Math.Min(seeds.Length, MaxEvents);
        var builder = ImmutableArray.CreateBuilder<EvaluationEvent>(count);

        for (var index = 0; index < count; index++)
        {
            var seed = seeds[index];
            builder.Add(
                new EvaluationEvent(
                    "event-" + index.ToString(CultureInfo.InvariantCulture),
                    UserId(seed),
                    new MovieId(1 + (int)((uint)seed % catalogSize)),
                    BaseTime.AddDays((uint)seed % 7).AddMinutes((uint)seed % 5)));
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Events that are strictly later than every event produced by
    /// <see cref="Events"/>, so they can only enter history for observations
    /// that come after them.
    /// </summary>
    public static ImmutableArray<EvaluationEvent> FutureEvents(EvaluationFixture fixture, int seed)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var catalogSize = fixture.Catalog.Length;
        var builder = ImmutableArray.CreateBuilder<EvaluationEvent>(4);

        for (var index = 0; index < 4; index++)
        {
            var eventSeed = seed + index;
            builder.Add(
                new EvaluationEvent(
                    "future-" + index.ToString(CultureInfo.InvariantCulture),
                    UserId(eventSeed),
                    new MovieId(1 + (int)((uint)eventSeed % catalogSize)),
                    BaseTime.AddDays(100).AddDays(index)));
        }

        return builder.ToImmutable();
    }

    public static ImmutableArray<EvaluationObservation> Observations(int[] targetSeeds, int[] rankingSeeds)
    {
        var count = Math.Min(
            Math.Min(targetSeeds.Length, rankingSeeds.Length),
            MaxObservationCount);
        var builder = ImmutableArray.CreateBuilder<EvaluationObservation>(count);

        for (var index = 0; index < count; index++)
        {
            builder.Add(
                new EvaluationObservation(
                    new MovieId(1 + (int)((uint)targetSeeds[index] % MaxMovieId)),
                    Ranking(rankingSeeds[index])));
        }

        return builder.ToImmutable();
    }

    public static double Lambda(int seed) => ((uint)seed % 101) / 100.0;

    private static ImmutableArray<MovieId> Ranking(int seed)
    {
        var length = (int)((uint)seed % (MaxRankingLength + 1));
        var builder = ImmutableArray.CreateBuilder<MovieId>(length);
        var usedIds = new HashSet<long>();
        var state = (uint)seed;

        while (builder.Count < length)
        {
            state = (state * 1664525u) + 1013904223u;
            var movieId = 1 + (int)(state % MaxMovieId);

            if (usedIds.Add(movieId))
            {
                builder.Add(new MovieId(movieId));
            }
        }

        return builder.ToImmutable();
    }

    private static MovieFeatures Features(int seed)
    {
        var normalized = seed & int.MaxValue;
        var genreCount = (normalized % GenreTokens.Length) + 1;
        var offset = (normalized / GenreTokens.Length) % GenreTokens.Length;
        var genres = new string[genreCount];

        for (var index = 0; index < genreCount; index++)
        {
            genres[index] = GenreTokens[(offset + index) % GenreTokens.Length];
        }

        return new MovieFeatures(genres: genres, directors: [GenreTokens[offset]]);
    }

    private static string UserId(int seed) =>
        "user-" + ((uint)seed % 3).ToString(CultureInfo.InvariantCulture);
}
