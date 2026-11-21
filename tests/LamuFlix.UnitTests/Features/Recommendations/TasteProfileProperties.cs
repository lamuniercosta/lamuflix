using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class TasteProfileProperties
{
    private const double GenreTolerance = 1e-12;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly string[] GenrePool = ["Action", " action", "Drama ", "SciFi", "sci-fi", "Comedy"];

    private static GenreWeights Weights => GenreWeights.Calculate(
        new GenreCounts(
            1000,
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
            {
                ["Action"] = 50,
                ["Drama"] = 200,
                ["SciFi"] = 500,
                ["Comedy"] = 900,
            }));

    [Fact]
    [Trait("Category", "Property")]
    public void Calculate_GeneratedHistory_ProducesFiniteConvexChannelValues() =>
        Prop.ForAll(
            HistoryGen(minPlayedAt: 0).ToArbitrary(),
            history =>
            {
                var profile = Calculator().Calculate(history.Movies, Weights);

                AssertGenreBounds(history.Movies, profile);
                AssertNoEmbeddingPayload(profile);
                return true;
            }).QuickCheckThrowOnFailure();

    [Fact]
    [Trait("Category", "Property")]
    public void Calculate_IdenticalSource_IsIdempotent() =>
        Prop.ForAll(
            HistoryGen(minPlayedAt: 0).ToArbitrary(),
            history =>
            {
                var calculator = Calculator();
                var first = calculator.Calculate(history.Movies, Weights);
                var second = calculator.Calculate(history.Movies, Weights);

                AssertProfilesEqual(first, second, 0.0);
                return true;
            }).QuickCheckThrowOnFailure();

    [Fact]
    [Trait("Category", "Property")]
    public void Calculate_FixedClock_IsDeterministicAcrossCalls() =>
        Prop.ForAll(
            HistoryGen(minPlayedAt: 0).ToArbitrary(),
            history =>
            {
                var first = Calculator().Calculate(history.Movies, Weights);
                var second = Calculator().Calculate(history.Movies, Weights);

                AssertProfilesEqual(first, second, 0.0);
                return true;
            }).QuickCheckThrowOnFailure();

    [Fact]
    [Trait("Category", "Property")]
    public void Calculate_UniformlyReplicatedWatchSignals_IsInvariant() =>
        Prop.ForAll(
            HistoryGen(minPlayedAt: 1).ToArbitrary(),
            history =>
            {
                var replicated = history.Movies
                    .Select(movie => movie with { PlayedAt = [.. movie.PlayedAt, .. movie.PlayedAt] })
                    .ToImmutableArray();

                var original = Calculator().Calculate(history.Movies, Weights);
                var doubled = Calculator().Calculate(replicated, Weights);

                AssertProfilesEqual(original, doubled, GenreTolerance);
                return true;
            }).QuickCheckThrowOnFailure();

    private static void AssertGenreBounds(ImmutableArray<TasteProfileMovie> movies, TasteProfile profile)
    {
        var genreValues = movies
            .Where(HasWatchSignal)
            .Where(movie => !movie.Genres.IsDefaultOrEmpty)
            .SelectMany(movie => movie.Genres)
            .Select(genre => Weights.GetWeight(genre.Trim()))
            .ToArray();

        if (genreValues.Length == 0)
        {
            profile.GenrePreferences.Count.ShouldBe(0);
            return;
        }

        foreach (var (genre, preference) in profile.GenrePreferences)
        {
            var rarity = Weights.GetWeight(genre);
            double.IsFinite(preference).ShouldBeTrue();
            (rarity > 0.0).ShouldBeTrue();
            (preference >= -GenreTolerance).ShouldBeTrue();
            (preference <= rarity + GenreTolerance).ShouldBeTrue();
        }
    }

    private static void AssertNoEmbeddingPayload(TasteProfile profile) =>
        profile.GetType().GetProperties()
            .Select(property => property.Name.ToLowerInvariant())
            .Any(name => name.Contains("embedding") || name.Contains("plot"))
            .ShouldBeFalse();

    private static void AssertProfilesEqual(
        TasteProfile expected,
        TasteProfile actual,
        double genreTolerance)
    {
        actual.GenrePreferences.Count.ShouldBe(expected.GenrePreferences.Count);

        foreach (var (genre, preference) in expected.GenrePreferences)
        {
            actual.GenrePreferences.ContainsKey(genre).ShouldBeTrue();
            actual.GenrePreferences[genre].ShouldBe(preference, genreTolerance);
        }

        actual.PlaybackByMovieId.Count.ShouldBe(expected.PlaybackByMovieId.Count);
    }

    private static bool HasWatchSignal(TasteProfileMovie movie) =>
        !movie.PlayedAt.IsDefaultOrEmpty || movie.IsWatched;

    private static TasteProfileCalculator Calculator() =>
        new(new FixedTimeProvider(Now), new RecommendationOptions());

    private static Gen<HistoryCase> HistoryGen(int minPlayedAt) =>
        from count in Gen.Choose(1, 4)
        from movies in Gen.CollectToList(Enumerable.Range(1, count), id => MovieGen(id, minPlayedAt))
        select new HistoryCase([.. movies]);

    private static Gen<TasteProfileMovie> MovieGen(int id, int minPlayedAt) =>
        from genreCount in Gen.Choose(0, 2)
        from genres in Gen.ListOf(Gen.Elements(GenrePool), genreCount)
        from embedding in EmbeddingGen()
        from playedAtCount in Gen.Choose(minPlayedAt, 3)
        from daysAgo in Gen.ListOf(Gen.Choose(-30, 400), playedAtCount)
        from isWatched in Gen.Elements(false, true)
        select new TasteProfileMovie(
            new MovieId(id),
            [.. genres],
            embedding,
            [.. daysAgo.Select(days => Now.AddDays(-days))],
            isWatched);

    private static Gen<ImmutableArray<float>> EmbeddingGen() =>
        from present in Gen.Elements(false, true)
        from coordinates in Gen.ArrayOf(Gen.Choose(0, 2).Select(value => (float)value), EmbeddingDimensions.Value)
        select present ? ImmutableArray.Create(coordinates) : ImmutableArray<float>.Empty;

    private sealed record HistoryCase(ImmutableArray<TasteProfileMovie> Movies);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
