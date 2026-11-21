using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class Dev510MutationSurvivorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly GenreWeights Table = GenreWeights.Calculate(
        new GenreCounts(
            100,
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
            {
                ["Drama"] = 10,
                ["Comedy"] = 20,
                ["Horror"] = 40,
            }));

    [Fact]
    public void Constructor_NonPositiveLambda_ReportsRecencyMessage()
    {
        // arrange
        var options = new RecommendationOptions { RecencyDecayLambda = 0.0 };

        // act
        var thrown = Should.Throw<ArgumentOutOfRangeException>(
            () => new TasteProfileCalculator(new FixedTimeProvider(Now), options));

        // assert
        thrown.Message.ShouldContain("RecencyDecayLambda must be finite and greater than 0.0.");
    }

    [Fact]
    public void Calculate_NullMovieRow_ReportsNullEntriesMessage()
    {
        // arrange
        var movies = ImmutableArray.Create(
            Movie(1, ["Drama"], ImmutableArray<float>.Empty, [Now]),
            // ReSharper disable once NullableWarningSuppressionIsUsed - the null row is the case under test; Calculate must reject it.
            null!);

        // act
        var thrown = Should.Throw<ArgumentException>(
            () => Calculator().Calculate(movies, Table));

        // assert
        thrown.Message.ShouldContain("Movie rows cannot contain null entries.");
        thrown.ParamName.ShouldBe("movies");
    }

    [Fact]
    public void Calculate_DuplicateMovieIdRows_ReportsMovieIdMessage()
    {
        // arrange
        var movies = ImmutableArray.Create(
            Movie(1, ["Drama"], ImmutableArray<float>.Empty, [Now]),
            Movie(1, ["Comedy"], ImmutableArray<float>.Empty, [Now]));

        // act
        var thrown = Should.Throw<ArgumentException>(
            () => Calculator().Calculate(movies, Table));

        // assert
        thrown.Message.ShouldContain("Movie rows cannot repeat movie id 1.");
        thrown.ParamName.ShouldBe("movies");
    }

    [Fact]
    public void Calculate_WrongLengthEmbedding_ReportsCoordinateCountMessage()
    {
        // arrange
        var movies = ImmutableArray.Create(
            Movie(1, ImmutableArray<string>.Empty, [1.0f, 2.0f], [Now]));

        // act
        var thrown = Should.Throw<ArgumentException>(
            () => Calculator().Calculate(movies, Table));

        // assert
        thrown.Message.ShouldContain($"Plot embeddings must contain exactly {EmbeddingDimensions.Value} coordinates.");
    }

    [Fact]
    public void Calculate_NonFiniteEmbeddingCoordinate_ReportsFiniteCoordinatesMessage()
    {
        // arrange
        var values = new float[EmbeddingDimensions.Value];
        values[0] = float.NaN;
        var movies = ImmutableArray.Create(
            Movie(1, ImmutableArray<string>.Empty, [.. values], [Now]));

        // act
        var thrown = Should.Throw<ArgumentException>(
            () => Calculator().Calculate(movies, Table));

        // assert
        thrown.Message.ShouldContain("Plot embeddings must contain only finite coordinates.");
    }

    private static TasteProfileMovie Movie(
        int id,
        ImmutableArray<string> genres,
        ImmutableArray<float> embedding,
        ImmutableArray<DateTimeOffset> playedAt) =>
        new(new MovieId(id), genres, embedding, playedAt, false);

    private static TasteProfileCalculator Calculator() =>
        new(new FixedTimeProvider(Now), new RecommendationOptions());
}
