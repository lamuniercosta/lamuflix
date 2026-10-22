using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Shouldly;
using Xunit;

namespace LamuFlix.Test.Features.Recommendations;

public sealed class TasteProfileProperties
{
    private const double GenreTolerance = 1e-12;
    private const float PlotTolerance = 1e-5f;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly string[] GenrePool = ["Action", " action", "Drama ", "SciFi", "sci-fi", "Comedy"];

    private static readonly GenreWeights Weights = GenreWeights.Calculate(
        new GenreCounts(
            1000,
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
            {
                ["Action"] = 50,
                ["Drama"] = 200,
                ["SciFi"] = 500,
                ["Comedy"] = 900,
            }));

    [Property]
    [Trait("Category", "Property")]
    public void Calculate_GeneratedHistory_ProducesFiniteConvexChannelValues() =>
        Prop.ForAll(
            HistoryGen(minPlayedAt: 0).ToArbitrary(),
            history =>
            {
                var profile = Calculator().Calculate(history.Movies, Weights);

                AssertGenreBounds(history.Movies, profile);
                AssertPlotBounds(history.Movies, profile);
                return true;
            }).QuickCheckThrowOnFailure();

    [Property]
    [Trait("Category", "Property")]
    public void Calculate_IdenticalSource_IsIdempotent() =>
        Prop.ForAll(
            HistoryGen(minPlayedAt: 0).ToArbitrary(),
            history =>
            {
                var calculator = Calculator();
                var first = calculator.Calculate(history.Movies, Weights);
                var second = calculator.Calculate(history.Movies, Weights);

                AssertProfilesEqual(first, second, 0.0, 0.0f);
                return true;
            }).QuickCheckThrowOnFailure();

    [Property]
    [Trait("Category", "Property")]
    public void Calculate_FixedClock_IsDeterministicAcrossCalls() =>
        Prop.ForAll(
            HistoryGen(minPlayedAt: 0).ToArbitrary(),
            history =>
            {
                var first = Calculator().Calculate(history.Movies, Weights);
                var second = Calculator().Calculate(history.Movies, Weights);

                AssertProfilesEqual(first, second, 0.0, 0.0f);
                return true;
            }).QuickCheckThrowOnFailure();

    [Property]
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

                AssertProfilesEqual(original, doubled, GenreTolerance, PlotTolerance);
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

    private static void AssertPlotBounds(ImmutableArray<TasteProfileMovie> movies, TasteProfile profile)
    {
        var contributors = movies
            .Where(HasWatchSignal)
            .Where(movie => !movie.PlotEmbedding.IsDefaultOrEmpty)
            .ToArray();

        if (contributors.Length == 0 || profile.PlotEmbedding.IsEmpty)
        {
            return;
        }

        profile.PlotEmbedding.Length.ShouldBe(EmbeddingDimensions.Value);

        for (var index = 0; index < EmbeddingDimensions.Value; index++)
        {
            var values = contributors.Select(movie => movie.PlotEmbedding[index]).ToArray();
            var coordinate = profile.PlotEmbedding[index];

            float.IsFinite(coordinate).ShouldBeTrue();
            (coordinate >= values.Min() - PlotTolerance).ShouldBeTrue();
            (coordinate <= values.Max() + PlotTolerance).ShouldBeTrue();
        }
    }

    private static void AssertProfilesEqual(
        TasteProfile expected,
        TasteProfile actual,
        double genreTolerance,
        float plotTolerance)
    {
        actual.GenrePreferences.Count.ShouldBe(expected.GenrePreferences.Count);

        foreach (var (genre, preference) in expected.GenrePreferences)
        {
            actual.GenrePreferences.ContainsKey(genre).ShouldBeTrue();
            actual.GenrePreferences[genre].ShouldBe(preference, genreTolerance);
        }

        actual.PlotEmbedding.IsEmpty.ShouldBe(expected.PlotEmbedding.IsEmpty);
        if (expected.PlotEmbedding.IsEmpty)
        {
            return;
        }

        actual.PlotEmbedding.Length.ShouldBe(expected.PlotEmbedding.Length);
        for (var index = 0; index < expected.PlotEmbedding.Length; index++)
        {
            actual.PlotEmbedding[index].ShouldBe(expected.PlotEmbedding[index], plotTolerance);
        }
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
