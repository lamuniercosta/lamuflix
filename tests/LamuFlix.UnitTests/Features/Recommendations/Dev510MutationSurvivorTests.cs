using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.UnitTests.Features;

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
    public void LikedReviewSource_From_NullChannels_ThrowsArgumentNullException()
    {
        // arrange
        var channels = ReviewProfileChannels.Empty;

        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed - the null channels is the case under test; From must reject it.
        Should.Throw<ArgumentNullException>(() => LikedReviewSource.From(new MovieId(1), "Title", null!));
        LikedReviewSource.From(new MovieId(1), "Title", channels).Genres.ShouldBeEmpty();
    }

    [Fact]
    public void EvaluateReviewAffinity_NullProfile_ThrowsArgumentNullException()
    {
        // arrange
        var embeddings = ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty;

        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed - the null profile is the case under test; EvaluateReviewAffinity must reject it.
        Should.Throw<ArgumentNullException>(() => ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            ImmutableArray<float>.Empty,
            null!,
            embeddings,
            new SimilarityWeights(),
            Table));
    }

    [Fact]
    public void EvaluateReviewAffinity_NullLikedSourceEmbeddings_ThrowsArgumentNullException()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty);

        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed - the null embeddings map is the case under test; EvaluateReviewAffinity must reject it.
        Should.Throw<ArgumentNullException>(() => ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            ImmutableArray<float>.Empty,
            profile,
            null!,
            new SimilarityWeights(),
            Table));
    }

    [Fact]
    public void EvaluateReviewAffinity_NullWeights_ThrowsArgumentNullException()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty);

        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed - the null weights is the case under test; EvaluateReviewAffinity must reject it.
        Should.Throw<ArgumentNullException>(() => ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            ImmutableArray<float>.Empty,
            profile,
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty,
            null!,
            Table));
    }

    [Fact]
    public void EvaluateReviewAffinity_NullGenreWeights_ThrowsArgumentNullException()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty);

        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed - the null genre weights is the case under test; EvaluateReviewAffinity must reject it.
        Should.Throw<ArgumentNullException>(() => ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            ImmutableArray<float>.Empty,
            profile,
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty,
            new SimilarityWeights(),
            null!));
    }

    [Fact]
    public void EvaluateReviewAffinity_TiedLikedSources_PrefersLowerMovieId()
    {
        // arrange
        var first = TiedProfile(
            new LikedReviewSource(new MovieId(5), "Five", Genres(["Drama"]), Cast(["Actor A"]), Directors(["Director A"])),
            new LikedReviewSource(new MovieId(3), "Three", Genres(["Drama"]), Cast(["Actor A"]), Directors(["Director A"])));
        var second = TiedProfile(
            new LikedReviewSource(new MovieId(3), "Three", Genres(["Drama"]), Cast(["Actor A"]), Directors(["Director A"])),
            new LikedReviewSource(new MovieId(5), "Five", Genres(["Drama"]), Cast(["Actor A"]), Directors(["Director A"])));

        // act
        var lateLower = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            ImmutableArray<float>.Empty,
            first,
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty,
            new SimilarityWeights(),
            Table);
        var earlyLower = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            ImmutableArray<float>.Empty,
            second,
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty,
            new SimilarityWeights(),
            Table);

        // assert
        lateLower.LikedSource.HasValue.ShouldBeTrue();
        lateLower.LikedSource!.Value.MovieId.ShouldBe(new MovieId(3));
        earlyLower.LikedSource.HasValue.ShouldBeTrue();
        earlyLower.LikedSource!.Value.MovieId.ShouldBe(new MovieId(3));
    }

    [Fact]
    public void EvaluateReviewAffinity_DuplicateMovieIdSources_PrefersLaterSource()
    {
        // arrange
        var profile = TiedProfile(
            new LikedReviewSource(new MovieId(7), "First", Genres(["Drama"]), Cast(["Actor A"]), Directors(["Director A"])),
            new LikedReviewSource(new MovieId(7), "Second", Genres(["Drama"]), Cast(["Actor A"]), Directors(["Director A"])));

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            ImmutableArray<float>.Empty,
            profile,
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty,
            new SimilarityWeights(),
            Table);

        // assert
        evidence.LikedSource.HasValue.ShouldBeTrue();
        evidence.LikedSource!.Value.Title.ShouldBe("Second");
    }

    [Fact]
    public void EvaluateReviewAffinity_MatchingEmbeddings_AccumulatesAllFourChannels()
    {
        // arrange
        var vector = Vector(1.0f);
        var liked = new ReviewProfileChannels(
            vector,
            Genres(["Drama"]),
            Cast(["Actor A"]),
            Directors(["Director A"]));
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty)
        {
            Liked = liked,
        };

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ["Actor A"],
            ["Director A"],
            vector,
            profile,
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty,
            new SimilarityWeights(),
            Table);

        // assert
        evidence.HasReviewChannel.ShouldBeTrue();
        evidence.LikedAffinity.ShouldBe(1.0, 1e-9);
    }

    [Fact]
    public void EvaluateReviewAffinity_ZeroWeightGenre_IsIgnored()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty)
        {
            Liked = new ReviewProfileChannels(
                ImmutableArray<float>.Empty,
                ImmutableDictionary<string, double>.Empty.Add("Drama", 0.0),
                ImmutableDictionary<string, double>.Empty,
                ImmutableDictionary<string, double>.Empty),
        };

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ImmutableArray<string>.Empty,
            ImmutableArray<string>.Empty,
            ImmutableArray<float>.Empty,
            profile,
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty,
            new SimilarityWeights(),
            Table);

        // assert
        evidence.HasReviewChannel.ShouldBeFalse();
        evidence.LikedAffinity.ShouldBe(0.0);
    }

    [Fact]
    public void EvaluateReviewAffinity_WhitespaceGenre_IsIgnored()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty)
        {
            Liked = new ReviewProfileChannels(
                ImmutableArray<float>.Empty,
                Genres(["Drama"]).Add(" ", 5.0),
                ImmutableDictionary<string, double>.Empty,
                ImmutableDictionary<string, double>.Empty),
        };

        // act
        var evidence = ProfileSimilarityCalculator.EvaluateReviewAffinity(
            ["Drama"],
            ImmutableArray<string>.Empty,
            ImmutableArray<string>.Empty,
            ImmutableArray<float>.Empty,
            profile,
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty,
            new SimilarityWeights(),
            Table);

        // assert
        evidence.HasReviewChannel.ShouldBeTrue();
        evidence.LikedAffinity.ShouldBe(1.0, 1e-9);
    }

    [Fact]
    public void Evaluate_ZeroPreferenceGenre_IsExcludedFromOverlapping()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty.Add("Drama", 1.0).Add("Comedy", 0.0),
            ImmutableArray<float>.Empty);

        // act
        var evidence = ProfileSimilarityCalculator.Evaluate(
            ["Drama", "Comedy"],
            ImmutableArray<float>.Empty,
            profile,
            Table);

        // assert
        evidence.Similarity.ShouldBeGreaterThan(0.0);
        evidence.OverlappingGenres.ShouldBe(["Drama"]);
    }

    [Fact]
    public void Evaluate_WrongLengthEmbedding_ThrowsWithCoordinateCountMessage()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty);

        // act
        var thrown = Should.Throw<ArgumentException>(() => ProfileSimilarityCalculator.Evaluate(
            ["Drama"],
            [1.0f, 2.0f, 3.0f],
            profile,
            Table));

        // assert
        thrown.Message.ShouldContain($"Plot embeddings must contain exactly {EmbeddingDimensions.Value} coordinates.");
        thrown.ParamName.ShouldBe("candidateEmbedding");
    }

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

    private static TasteProfile TiedProfile(LikedReviewSource first, LikedReviewSource second) =>
        new(ImmutableDictionary<string, double>.Empty, ImmutableArray<float>.Empty)
        {
            Liked = new ReviewProfileChannels(
                ImmutableArray<float>.Empty,
                Genres(["Drama"]),
                Cast(["Actor A"]),
                Directors(["Director A"])),
            LikedSources = [first, second],
        };

    private static ImmutableDictionary<string, double> Genres(string[] names) =>
        names.ToImmutableDictionary(name => name, Table.GetWeight, StringComparer.OrdinalIgnoreCase);

    private static ImmutableDictionary<string, double> Cast(string[] names) =>
        names.ToImmutableDictionary(name => name, _ => 1.0, StringComparer.OrdinalIgnoreCase);

    private static ImmutableDictionary<string, double> Directors(string[] names) =>
        names.ToImmutableDictionary(name => name, _ => 1.0, StringComparer.OrdinalIgnoreCase);

    private static ImmutableArray<float> Vector(float value)
    {
        var builder = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);

        for (var index = 0; index < EmbeddingDimensions.Value; index++)
        {
            builder.Add(value);
        }

        return builder.MoveToImmutable();
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
