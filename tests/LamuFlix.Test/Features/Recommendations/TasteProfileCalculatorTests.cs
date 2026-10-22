using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Shouldly;
using Xunit;

namespace LamuFlix.Test.Features.Recommendations;

public sealed class TasteProfileCalculatorTests
{
    private const double Tolerance = 1e-12;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Calculate_TwoMoviesAgesZeroAndThirtyDays_UsesTwoThirdsOneThirdMixture()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("gA", 1), ("gB", 9));
        var rarityA = weights.GetWeight("gA");
        var rarityB = weights.GetWeight("gB");
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, [Now]),
            Movie(2, ["gB"], ImmutableArray<float>.Empty, [Now.AddDays(-30)]));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.GenrePreferences.Count.ShouldBe(2);
        profile.GenrePreferences["gA"].ShouldBe((2.0 / 3.0) * rarityA, Tolerance);
        profile.GenrePreferences["gB"].ShouldBe((1.0 / 3.0) * rarityB, Tolerance);
        profile.PlotEmbedding.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Calculate_FractionalAge_UsesExponentialDecay()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("gA", 1), ("gB", 9));
        var rarityA = weights.GetWeight("gA");
        var rarityB = weights.GetWeight("gB");
        var lambda = new RecommendationOptions().RecencyDecayLambda;
        var decay = Math.Exp(-lambda * 2.5);
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, [Now]),
            Movie(2, ["gB"], ImmutableArray<float>.Empty, [Now.AddDays(-2.5)]));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.GenrePreferences["gA"].ShouldBe((1.0 / (1.0 + decay)) * rarityA, Tolerance);
        profile.GenrePreferences["gB"].ShouldBe((decay / (1.0 + decay)) * rarityB, Tolerance);
    }

    [Fact]
    public void Calculate_OffsetEquivalentTimestamps_ProducesSameProfile()
    {
        // arrange
        var weights = Weights(100, ("gA", 1), ("gB", 9));
        var utcMovies = ImmutableArray.Create(
            Movie(1, ["gA"], Basis(0, 1.0f), [Now]),
            Movie(2, ["gB"], Basis(1, 1.0f), [Now.AddDays(-30)]));
        var offsetMovies = ImmutableArray.Create(
            Movie(1, ["gA"], Basis(0, 1.0f), [Now.ToOffset(TimeSpan.FromHours(5))]),
            Movie(2, ["gB"], Basis(1, 1.0f), [Now.AddDays(-30).ToOffset(TimeSpan.FromHours(-7))]));

        // act
        var utc = Calculator(new CountingTimeProvider(Now)).Calculate(utcMovies, weights);
        var offset = Calculator(new CountingTimeProvider(Now)).Calculate(offsetMovies, weights);

        // assert
        offset.GenrePreferences["gA"].ShouldBe(utc.GenrePreferences["gA"], Tolerance);
        offset.GenrePreferences["gB"].ShouldBe(utc.GenrePreferences["gB"], Tolerance);
        for (var index = 0; index < EmbeddingDimensions.Value; index++)
        {
            offset.PlotEmbedding[index].ShouldBe(utc.PlotEmbedding[index]);
        }
    }

    [Fact]
    public void Calculate_DuplicateSameInstantLaunches_RetainsEachSignal()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("gA", 1), ("gB", 9));
        var rarityA = weights.GetWeight("gA");
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, [Now, Now]),
            Movie(2, ["gB"], ImmutableArray<float>.Empty, [Now.AddDays(-30)]));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.GenrePreferences["gA"].ShouldBe((2.0 / 2.5) * rarityA, Tolerance);
    }

    [Fact]
    public void Calculate_MovieWithLaunchesAndWatchedMark_DoesNotDoubleCount()
    {
        // arrange
        var weights = Weights(100, ("gA", 1), ("gB", 9));
        var playedAt = ImmutableArray.Create(Now, Now.AddDays(-30));
        var marked = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, playedAt, isWatched: true));
        var unmarked = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, playedAt, isWatched: false));

        // act
        var markedProfile = Calculator(new CountingTimeProvider(Now)).Calculate(marked, weights);
        var unmarkedProfile = Calculator(new CountingTimeProvider(Now)).Calculate(unmarked, weights);

        // assert
        markedProfile.GenrePreferences["gA"].ShouldBe(unmarkedProfile.GenrePreferences["gA"], Tolerance);
        unmarkedProfile.GenrePreferences["gA"].ShouldBe(weights.GetWeight("gA"), Tolerance);
    }

    [Fact]
    public void Calculate_WatchedMarkWithoutLaunches_ContributesOneAgeZeroSignal()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("gA", 1), ("gB", 9));
        var rarityA = weights.GetWeight("gA");
        var rarityB = weights.GetWeight("gB");
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, ImmutableArray<DateTimeOffset>.Empty, isWatched: true),
            Movie(2, ["gB"], ImmutableArray<float>.Empty, [Now.AddDays(-30)]));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.GenrePreferences["gA"].ShouldBe((2.0 / 3.0) * rarityA, Tolerance);
        profile.GenrePreferences["gB"].ShouldBe((1.0 / 3.0) * rarityB, Tolerance);
    }

    [Fact]
    public void Calculate_LaunchesWithUnwatchedMark_StillContribute()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("gA", 1));
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, [Now.AddDays(-30)], isWatched: false));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.GenrePreferences["gA"].ShouldBe(weights.GetWeight("gA"), Tolerance);
    }

    [Fact]
    public void Calculate_GenreAndPlotContributorsDiffer_KeepsIndependentDenominators()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("gA", 1));
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, [Now]),
            Movie(2, ImmutableArray<string>.Empty, Basis(0, 1.0f), [Now.AddDays(-30)]));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.GenrePreferences.Count.ShouldBe(1);
        profile.GenrePreferences["gA"].ShouldBe(weights.GetWeight("gA"), Tolerance);
        profile.PlotEmbedding.Length.ShouldBe(EmbeddingDimensions.Value);
        profile.PlotEmbedding[0].ShouldBe(1.0f);
        profile.PlotEmbedding[1].ShouldBe(0.0f);
    }

    [Fact]
    public void Calculate_GenresDifferByCaseAndWhitespace_CollapsesToOrdinalMinimumKey()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("drama", 1));
        var movies = ImmutableArray.Create(
            Movie(1, [" drama ", "Drama", "DRAMA", string.Empty, "   "], ImmutableArray<float>.Empty, [Now]));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.GenrePreferences.Count.ShouldBe(1);
        profile.GenrePreferences.ShouldContainKey("drama");
        profile.GenrePreferences.Keys.Single().ShouldBe("DRAMA");
        profile.GenrePreferences["drama"].ShouldBe(weights.GetWeight("drama"), Tolerance);
        profile.GenrePreferences.KeyComparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_GenresDifferAcrossMovies_CollapsesToOrdinalMinimumKey()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("drama", 1));
        var movies = ImmutableArray.Create(
            Movie(1, ["drama"], ImmutableArray<float>.Empty, [Now]),
            Movie(2, ["Drama"], ImmutableArray<float>.Empty, [Now]));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.GenrePreferences.Count.ShouldBe(1);
        profile.GenrePreferences.Keys.Single().ShouldBe("Drama");
    }

    [Fact]
    public void Calculate_MovieWithTwoGenres_DoesNotDivideRarityByGenreCount()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("gA", 1), ("gB", 9));
        var movies = ImmutableArray.Create(
            Movie(1, ["gA", "gB"], ImmutableArray<float>.Empty, [Now]));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.GenrePreferences["gA"].ShouldBe(weights.GetWeight("gA"), Tolerance);
        profile.GenrePreferences["gB"].ShouldBe(weights.GetWeight("gB"), Tolerance);
    }

    [Fact]
    public void Calculate_UnnormalizedBasisEmbeddings_ProducesUnnormalizedWeightedMean()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var movies = ImmutableArray.Create(
            Movie(1, ImmutableArray<string>.Empty, Basis(0, 3.0f), [Now]),
            Movie(2, ImmutableArray<string>.Empty, Basis(1, 2.0f), [Now.AddDays(-30)]));

        // act
        var profile = Calculator(clock).Calculate(movies, Weights(10));

        // assert
        profile.PlotEmbedding.Length.ShouldBe(EmbeddingDimensions.Value);
        profile.PlotEmbedding[0].ShouldBe(2.0f, 1e-6f);
        profile.PlotEmbedding[1].ShouldBe(2.0f / 3.0f, 1e-6f);
        for (var index = 2; index < EmbeddingDimensions.Value; index++)
        {
            profile.PlotEmbedding[index].ShouldBe(0.0f);
        }
    }

    [Fact]
    public void Calculate_SingleCall_SamplesClockExactlyOnce()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], Basis(0, 1.0f), [Now, Now.AddDays(-30)]));

        // act
        Calculator(clock).Calculate(movies, Weights(100, ("gA", 1)));

        // assert
        clock.UtcNowCallCount.ShouldBe(1);
    }

    [Fact]
    public void Calculate_RepeatedCalls_ProduceBitwiseIdenticalProfile()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("gA", 1), ("gB", 9));
        var movies = ImmutableArray.Create(
            Movie(1, ["gA", "gB"], Basis(0, 3.0f), [Now, Now.AddDays(-7)]),
            Movie(2, ["gB"], Basis(1, 2.0f), [Now.AddDays(-30)]));
        var calculator = Calculator(clock);

        // act
        var first = calculator.Calculate(movies, weights);
        var second = calculator.Calculate(movies, weights);

        // assert
        clock.UtcNowCallCount.ShouldBe(2);
        second.GenrePreferences.Count.ShouldBe(first.GenrePreferences.Count);
        foreach (var entry in first.GenrePreferences)
        {
            second.GenrePreferences[entry.Key].ShouldBe(entry.Value);
        }

        second.PlotEmbedding.Length.ShouldBe(first.PlotEmbedding.Length);
        for (var index = 0; index < first.PlotEmbedding.Length; index++)
        {
            second.PlotEmbedding[index].ShouldBe(first.PlotEmbedding[index]);
        }
    }

    [Fact]
    public void Calculate_ValidInput_DoesNotMutateCallerInputs()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var genres = ImmutableArray.Create(" gA ", "gB");
        var embedding = Basis(3, 4.0f);
        var playedAt = ImmutableArray.Create(Now, Now.AddDays(-30), Now.AddDays(-30));
        var movies = ImmutableArray.Create(Movie(1, genres, embedding, playedAt));

        // act
        Calculator(clock).Calculate(movies, Weights(100, ("gA", 1), ("gB", 9)));

        // assert
        genres.ShouldBe([" gA ", "gB"]);
        for (var index = 0; index < EmbeddingDimensions.Value; index++)
        {
            embedding[index].ShouldBe(index == 3 ? 4.0f : 0.0f);
        }

        playedAt.Length.ShouldBe(3);
        playedAt[1].ShouldBe(Now.AddDays(-30));
        playedAt[2].ShouldBe(Now.AddDays(-30));
    }

    [Fact]
    public void Calculate_EmptyMoviesArray_ReturnsEmptyProfileWithoutThrow()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(10, ("gA", 1));

        // act
        var profile = Calculator(clock).Calculate(ImmutableArray<TasteProfileMovie>.Empty, weights);

        // assert
        profile.IsEmpty.ShouldBeTrue();
        profile.GenrePreferences.IsEmpty.ShouldBeTrue();
        profile.PlotEmbedding.IsEmpty.ShouldBeTrue();
        clock.UtcNowCallCount.ShouldBe(1);
    }

    [Fact]
    public void Calculate_DefaultMoviesArray_ReturnsEmptyProfileWithoutThrow()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(10, ("gA", 1));

        // act
        var profile = Calculator(clock).Calculate(default, weights);

        // assert
        profile.IsEmpty.ShouldBeTrue();
        clock.UtcNowCallCount.ShouldBe(1);
    }

    [Fact]
    public void Calculate_RowsWithoutLaunchesOrWatchedMark_ReturnsEmptyProfileEvenWithMetadata()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("gA", 1));
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], Basis(0, 5.0f), ImmutableArray<DateTimeOffset>.Empty, isWatched: false),
            Movie(2, ["gB"], Basis(1, 4.0f), ImmutableArray<DateTimeOffset>.Empty, isWatched: false));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.IsEmpty.ShouldBeTrue();
        profile.GenrePreferences.IsEmpty.ShouldBeTrue();
        profile.PlotEmbedding.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Calculate_GenreOnlyHistory_LeavesPlotEmpty()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("gA", 1));
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, [Now]),
            Movie(2, ImmutableArray<string>.Empty, ImmutableArray<float>.Empty, [Now.AddDays(-30)]));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.IsEmpty.ShouldBeFalse();
        profile.GenrePreferences.Count.ShouldBe(1);
        profile.GenrePreferences["gA"].ShouldBe(weights.GetWeight("gA"), Tolerance);
        profile.PlotEmbedding.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Calculate_PlotOnlyHistory_LeavesGenrePreferencesEmpty()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var movies = ImmutableArray.Create(
            Movie(1, ImmutableArray<string>.Empty, Basis(0, 2.0f), [Now]),
            Movie(2, ImmutableArray<string>.Empty, Basis(0, 4.0f), [Now.AddDays(-30)]));

        // act
        var profile = Calculator(clock).Calculate(movies, Weights(10));

        // assert
        profile.IsEmpty.ShouldBeFalse();
        profile.GenrePreferences.IsEmpty.ShouldBeTrue();
        profile.PlotEmbedding.Length.ShouldBe(EmbeddingDimensions.Value);
        profile.PlotEmbedding[0].ShouldBe((2.0f + (0.5f * 4.0f)) / 1.5f, 1e-6f);
    }

    [Fact]
    public void Calculate_MissingEmbeddings_AreOmittedFromPlotDenominator()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var movies = ImmutableArray.Create(
            Movie(1, ImmutableArray<string>.Empty, Basis(0, 1.0f), [Now]),
            Movie(2, ImmutableArray<string>.Empty, ImmutableArray<float>.Empty, [Now]),
            Movie(3, ImmutableArray<string>.Empty, ImmutableArray<float>.Empty, [Now]));

        // act
        var profile = Calculator(clock).Calculate(movies, Weights(10));

        // assert
        profile.PlotEmbedding.Length.ShouldBe(EmbeddingDimensions.Value);
        profile.PlotEmbedding[0].ShouldBe(1.0f, 1e-6f);
    }

    [Fact]
    public void Calculate_ValidZeroVectors_AreIncludedInPlotDenominator()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var zero = new float[EmbeddingDimensions.Value];
        var movies = ImmutableArray.Create(
            Movie(1, ImmutableArray<string>.Empty, Basis(0, 1.0f), [Now]),
            Movie(2, ImmutableArray<string>.Empty, [.. zero], [Now]));

        // act
        var profile = Calculator(clock).Calculate(movies, Weights(10));

        // assert
        profile.PlotEmbedding.Length.ShouldBe(EmbeddingDimensions.Value);
        profile.PlotEmbedding[0].ShouldBe(0.5f, 1e-6f);
    }

    [Fact]
    public void Calculate_AllZeroMean_ReturnsEmptyPlotAndKeepsGenrePreferences()
    {
        // arrange
        var clock = new CountingTimeProvider(Now);
        var weights = Weights(100, ("gA", 1));
        var zero = new float[EmbeddingDimensions.Value];
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], [.. zero], [Now]));

        // act
        var profile = Calculator(clock).Calculate(movies, weights);

        // assert
        profile.IsEmpty.ShouldBeFalse();
        profile.PlotEmbedding.IsEmpty.ShouldBeTrue();
        profile.GenrePreferences.Count.ShouldBe(1);
        profile.GenrePreferences["gA"].ShouldBe(weights.GetWeight("gA"), Tolerance);
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // act / assert
        Should.Throw<ArgumentNullException>(() => new TasteProfileCalculator(null!, new RecommendationOptions()));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // act / assert
        Should.Throw<ArgumentNullException>(() => new TasteProfileCalculator(new CountingTimeProvider(Now), null!));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0.0)]
    [InlineData(-0.0001)]
    public void Constructor_NonFiniteOrNonPositiveLambda_ThrowsArgumentOutOfRangeException(double lambda)
    {
        // arrange
        var options = new RecommendationOptions { RecencyDecayLambda = lambda };

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() => new TasteProfileCalculator(new CountingTimeProvider(Now), options));
    }

    [Fact]
    public void Constructor_ValidLambdaWithInvalidUnrelatedOptions_DoesNotThrow()
    {
        // arrange
        var options = new RecommendationOptions
        {
            RecencyDecayLambda = 0.01,
            MmrLambda = -1.0,
            Take = 0,
            CandidatePoolSize = 0,
        };

        // act
        var calculator = new TasteProfileCalculator(new CountingTimeProvider(Now), options);

        // assert
        calculator.ShouldNotBeNull();
    }

    [Fact]
    public void Calculate_NullGenreWeights_ThrowsArgumentNullException()
    {
        // arrange
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, [Now]));

        // act / assert
        Should.Throw<ArgumentNullException>(() => Calculator(new CountingTimeProvider(Now)).Calculate(movies, null!));
    }

    [Fact]
    public void Calculate_NullMovieRow_ThrowsArgumentException()
    {
        // arrange
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, [Now]),
            null!);

        // act / assert
        Should.Throw<ArgumentException>(() => Calculator(new CountingTimeProvider(Now)).Calculate(movies, Weights(10)));
    }

    [Fact]
    public void Calculate_DuplicateMovieIdRows_ThrowsArgumentException()
    {
        // arrange
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, [Now]),
            Movie(1, ["gB"], ImmutableArray<float>.Empty, [Now.AddDays(-30)]));

        // act / assert
        Should.Throw<ArgumentException>(() => Calculator(new CountingTimeProvider(Now)).Calculate(movies, Weights(10)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(383)]
    [InlineData(385)]
    public void Calculate_NonEmptyEmbeddingWithWrongLength_ThrowsArgumentException(int length)
    {
        // arrange
        var movies = ImmutableArray.Create(
            Movie(1, ImmutableArray<string>.Empty, [.. new float[length]], [Now]));

        // act / assert
        Should.Throw<ArgumentException>(() => Calculator(new CountingTimeProvider(Now)).Calculate(movies, Weights(10)));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void Calculate_NonFiniteEmbeddingCoordinate_ThrowsArgumentException(float coordinate)
    {
        // arrange
        var movies = ImmutableArray.Create(
            Movie(1, ImmutableArray<string>.Empty, Basis(0, coordinate), [Now]));

        // act / assert
        Should.Throw<ArgumentException>(() => Calculator(new CountingTimeProvider(Now)).Calculate(movies, Weights(10)));
    }

    [Fact]
    public void Calculate_CorruptedUnwatchedRowsWithoutWatchSignals_StillThrowBeforeAccumulation()
    {
        // arrange
        var corruptEmbedding = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, [Now]),
            Movie(2, ["gB"], [.. new float[2]], ImmutableArray<DateTimeOffset>.Empty, isWatched: false));
        var duplicateIds = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, ImmutableArray<DateTimeOffset>.Empty, isWatched: false),
            Movie(1, ["gB"], ImmutableArray<float>.Empty, ImmutableArray<DateTimeOffset>.Empty, isWatched: false));

        // act / assert
        Should.Throw<ArgumentException>(() => Calculator(new CountingTimeProvider(Now)).Calculate(corruptEmbedding, Weights(10)));
        Should.Throw<ArgumentException>(() => Calculator(new CountingTimeProvider(Now)).Calculate(duplicateIds, Weights(10)));
    }

    [Fact]
    public void Calculate_DefaultArraysOnValidRows_SucceedsWithoutThrow()
    {
        // arrange
        var movies = ImmutableArray.Create(
            new TasteProfileMovie(
                new MovieId(1),
                default,
                default,
                default,
                IsWatched: true));

        // act
        var profile = Calculator(new CountingTimeProvider(Now)).Calculate(movies, Weights(10));

        // assert
        profile.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Calculate_DuplicatePlayedAtEntries_ArePreservedByValidation()
    {
        // arrange
        var weights = Weights(100, ("gA", 1));
        var movies = ImmutableArray.Create(
            Movie(1, ["gA"], ImmutableArray<float>.Empty, [Now, Now, Now], isWatched: true));

        // act
        var profile = Calculator(new CountingTimeProvider(Now)).Calculate(movies, weights);

        // assert
        profile.GenrePreferences["gA"].ShouldBe(weights.GetWeight("gA"), Tolerance);
    }

    [Fact]
    public void Calculate_FailedValidation_DoesNotMutateCallerInputs()
    {
        // arrange
        var genres = ImmutableArray.Create(" gA ", "gB");
        var embedding = Basis(0, float.NaN);
        var playedAt = ImmutableArray.Create(Now, Now, Now.AddDays(-30));
        var movies = ImmutableArray.Create(
            Movie(1, genres, embedding, playedAt));

        // act
        Should.Throw<ArgumentException>(() => Calculator(new CountingTimeProvider(Now)).Calculate(movies, Weights(10)));

        // assert
        genres.ShouldBe([" gA ", "gB"]);
        float.IsNaN(embedding[0]).ShouldBeTrue();
        for (var index = 1; index < EmbeddingDimensions.Value; index++)
        {
            embedding[index].ShouldBe(0.0f);
        }

        playedAt.ShouldBe([Now, Now, Now.AddDays(-30)]);
    }

    private static TasteProfileCalculator Calculator(TimeProvider clock) =>
        new(clock, new RecommendationOptions());

    private static TasteProfileMovie Movie(
        int id,
        ImmutableArray<string> genres,
        ImmutableArray<float> embedding,
        ImmutableArray<DateTimeOffset> playedAt,
        bool isWatched = false) =>
        new(new MovieId(id), genres, embedding, playedAt, isWatched);

    private static ImmutableArray<float> Basis(int index, float magnitude)
    {
        var values = new float[EmbeddingDimensions.Value];
        values[index] = magnitude;
        return [.. values];
    }

    private static GenreWeights Weights(long totalMovies, params (string Genre, long Count)[] entries)
    {
        var counts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var (genre, count) in entries)
        {
            counts[genre] = count;
        }

        return GenreWeights.Calculate(new GenreCounts(totalMovies, counts));
    }

    private sealed class CountingTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public int UtcNowCallCount { get; private set; }

        public override DateTimeOffset GetUtcNow()
        {
            UtcNowCallCount++;
            return utcNow;
        }
    }
}
