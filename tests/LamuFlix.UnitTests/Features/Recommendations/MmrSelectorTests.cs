using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class MmrSelectorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Select_NullCandidateList_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => MmrSelector.Select(null!, 1, 0.7, Options(), GenreTable(), Counts()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_NullOptions_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => MmrSelector.Select(Pool(Candidate(1)), 1, 0.7, null!, GenreTable(), Counts()));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Select_NullGenreWeights_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => MmrSelector.Select(Pool(Candidate(1)), 1, 0.7, Options(), null!, Counts()));

        // assert
        failure.ParamName.ShouldBe("genreWeights");
    }

    [Fact]
    public void Select_NullCandidateItem_ThrowsArgumentException()
    {
        // arrange
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var candidates = new List<MmrCandidate> { Candidate(1), null! };

        // act
        var failure = Should.Throw<ArgumentException>(() => MmrSelector.Select(candidates, 1, 0.7, Options(), GenreTable(), Counts()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_NullCandidateMovieId_ThrowsArgumentException()
    {
        // arrange
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var candidates = Pool(new MmrCandidate(null!, 0.5, Features()));

        // act
        var failure = Should.Throw<ArgumentException>(() => MmrSelector.Select(candidates, 1, 0.7, Options(), GenreTable(), Counts()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_NullCandidateFeatures_ThrowsArgumentException()
    {
        // arrange
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var candidates = Pool(new MmrCandidate(new MovieId(1), 0.5, null!));

        // act
        var failure = Should.Throw<ArgumentException>(() => MmrSelector.Select(candidates, 1, 0.7, Options(), GenreTable(), Counts()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_DuplicateMovieIds_ThrowsArgumentException()
    {
        // arrange
        var candidates = Pool(Candidate(7), Candidate(7, relevance: 0.4));

        // act
        var failure = Should.Throw<ArgumentException>(() => MmrSelector.Select(candidates, 2, 0.7, Options(), GenreTable(), Counts()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_NegativeTake_ThrowsArgumentOutOfRangeException()
    {
        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(Pool(Candidate(1)), -1, 0.7, Options(), GenreTable(), Counts()));

        // assert
        failure.ParamName.ShouldBe("take");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Select_InvalidLambda_ThrowsArgumentOutOfRangeException(double lambda)
    {
        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(Pool(Candidate(1)), 1, lambda, Options(), GenreTable(), Counts()));

        // assert
        failure.ParamName.ShouldBe("lambda");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Select_InvalidRelevance_ThrowsArgumentOutOfRangeException(double relevance)
    {
        // arrange
        var candidates = Pool(new MmrCandidate(new MovieId(1), relevance, Features()));

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(candidates, 1, 0.7, Options(), GenreTable(), Counts()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_InvalidWeightOnAnyOfSixFields_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        SimilarityWeights[] weights =
        [
            new() { Collection = -1.0 },
            new() { Director = double.NaN },
            new() { Themes = -0.25 },
            new() { Plot = double.NaN },
            new() { Cast = double.PositiveInfinity },
            new() { Genres = double.NegativeInfinity },
        ];

        // act / assert
        foreach (var invalid in weights)
        {
            var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(Pool(Candidate(1)), 1, 0.7, new RecommendationOptions { Weights = invalid }, GenreTable(), Counts()));
            failure.ParamName.ShouldBe("weights");
        }
    }

    [Fact]
    public void Select_InvalidWeightsWithValidEmptyPool_StillThrows()
    {
        // arrange
        var weights = new SimilarityWeights { Genres = -1.0 };

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(Pool(), 1, 0.7, new RecommendationOptions { Weights = weights }, GenreTable(), Counts()));

        // assert
        failure.ParamName.ShouldBe("weights");
    }

    [Fact]
    public void Select_InvalidWeightsWithZeroTake_StillThrows()
    {
        // arrange
        var weights = new SimilarityWeights { Director = double.NaN };

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(Pool(Candidate(1)), 0, 0.7, new RecommendationOptions { Weights = weights }, GenreTable(), Counts()));

        // assert
        failure.ParamName.ShouldBe("weights");
    }

    [Fact]
    public void Select_ValidEmptyPool_ReturnsEmpty()
    {
        // act
        var result = MmrSelector.Select(Pool(), 5, 0.7, Options(), GenreTable(), Counts());

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Select_ZeroTake_ReturnsEmpty()
    {
        // act
        var result = MmrSelector.Select(Pool(Candidate(1), Candidate(2)), 0, 0.7, Options(), GenreTable(), Counts());

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Select_OversizedTake_ReturnsEntirePool()
    {
        // arrange
        var candidates = Pool(Candidate(3), Candidate(1), Candidate(2));
        var expected = new[] { new MovieId(3), new MovieId(1), new MovieId(2) };

        // act
        var result = MmrSelector.Select(candidates, 10, 0.7, Options(genres: 1.0), GenreTable(), Counts());

        // assert
        result.Count.ShouldBe(3);
        result.ShouldBe(expected, ignoreOrder: true);
    }

    [Fact]
    public void Select_RelevanceAboveOne_IsAccepted()
    {
        // arrange
        var candidates = Pool(Candidate(1, relevance: 2.5));

        // act
        var result = MmrSelector.Select(candidates, 1, 0.7, Options(), GenreTable(), Counts());

        // assert
        result.ShouldBe([new MovieId(1)]);
    }

    [Fact]
    public void Select_LeavesCandidateListOrderAndContentUnchanged()
    {
        // arrange
        var candidates = Pool(Candidate(5), Candidate(2, relevance: 0.9), Candidate(9, relevance: 0.1));
        var before = candidates.ToArray();

        // act
        MmrSelector.Select(candidates, 2, 0.7, Options(genres: 1.0), GenreTable(), Counts());

        // assert
        candidates.ShouldBe(before);
    }

    [Fact]
    public void Select_LeavesFeaturesAndWeightsUnchanged()
    {
        // arrange
        var features = Features(genres: ["Drama"]);
        var candidates = Pool(new MmrCandidate(new MovieId(1), 0.5, features));
        var weights = Options(genres: 1.0);
        var genreWeights = GenreTable();
        var dramaWeight = genreWeights.GetWeight("Drama");

        // act
        MmrSelector.Select(candidates, 1, 0.7, weights, genreWeights, Counts());

        // assert
        candidates[0].Features.ShouldBeSameAs(features);
        candidates[0].Features.ShouldBe(features);
        weights.Weights.ShouldBe(Options(genres: 1.0).Weights);
        genreWeights.GetWeight("Drama").ShouldBe(dramaWeight);
    }

    [Fact]
    public void Select_InterleavedCallsWithDifferentLambdaAndTake_RetainsEarlierResult()
    {
        // arrange
        var candidates = Pool(Candidate(1, 0.9), Candidate(2, 0.8), Candidate(3, 0.7));
        var first = MmrSelector.Select(candidates, 2, 0.7, Options(genres: 1.0), GenreTable(), Counts());
        var firstSnapshot = first.ToArray();

        // act
        var second = MmrSelector.Select(candidates, 1, 0.2, Options(genres: 1.0), GenreTable(), Counts());
        var repeated = MmrSelector.Select(candidates, 2, 0.7, Options(genres: 1.0), GenreTable(), Counts());

        // assert
        second.ShouldBe([new MovieId(1)]);
        repeated.ShouldBe(firstSnapshot);
        first.ShouldBe(firstSnapshot);
    }

    [Fact]
    public void Select_LambdaOne_OrdersByRelevanceDescendingThenId()
    {
        // arrange
        var candidates = Pool(
            Candidate(3, relevance: 0.5),
            Candidate(1, relevance: 0.9),
            Candidate(2, relevance: 0.9),
            Candidate(4, relevance: 0.2));

        // act
        var result = MmrSelector.Select(candidates, 4, 1.0, Options(genres: 1.0), GenreTable(), Counts());

        // assert
        result.ShouldBe([new MovieId(1), new MovieId(2), new MovieId(3), new MovieId(4)]);
    }

    [Fact]
    public void Select_LambdaZero_PicksSmallestIdThenMinimizesMaximumRedundancy()
    {
        // arrange
        var candidates = Pool(
            Candidate(1, genres: ["A"]),
            Candidate(2, genres: ["A"]),
            Candidate(3, genres: ["B"]),
            Candidate(4, genres: ["B"]));

        // act
        var result = MmrSelector.Select(candidates, 4, 0.0, Options(genres: 1.0), UniformGenres(), Counts());

        // assert
        result.ShouldBe([new MovieId(1), new MovieId(3), new MovieId(2), new MovieId(4)]);
    }

    [Fact]
    public void Select_ExactObjectiveTies_AreStableAcrossInputPermutations()
    {
        // arrange
        int[][] permutations =
        [
            [1, 2, 3, 4],
            [4, 3, 2, 1],
            [2, 4, 1, 3],
            [3, 1, 4, 2],
        ];
        var expected = new[] { new MovieId(1), new MovieId(2), new MovieId(3), new MovieId(4) };

        // act / assert
        foreach (var permutation in permutations)
        {
            var candidates = Pool([.. permutation.Select(id => Candidate(id, relevance: 0.5, genres: ["Shared"]))]);
            var result = MmrSelector.Select(candidates, 4, 0.7, Options(genres: 1.0), UniformGenres(), Counts());
            result.ShouldBe(expected);
        }
    }

    [Fact]
    public void Select_AllZeroWeights_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var candidates = Pool(
            Candidate(1, genres: ["A"]),
            Candidate(2, genres: ["A"]),
            Candidate(3, genres: ["B"]));

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(
            () => MmrSelector.Select(candidates, 3, 0.0, Options(), UniformGenres(), Counts()));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Select_MissingFeatures_ContributeZeroRedundancy()
    {
        // arrange
        var candidates = Pool(
            Candidate(1, genres: ["A"]),
            new MmrCandidate(new MovieId(2), 0.5, Features()),
            Candidate(3, genres: ["B"]));

        // act
        var result = MmrSelector.Select(candidates, 3, 0.0, Options(genres: 1.0), UniformGenres(), Counts());

        // assert
        result.ShouldBe([new MovieId(1), new MovieId(2), new MovieId(3)]);
    }

    [Fact]
    public void Select_EarlierSelectedCandidateSuppliesDecisiveMaximumRedundancy()
    {
        // arrange
        var candidates = Pool(
            Candidate(1, genres: ["a"]),
            Candidate(2, genres: ["b"]),
            Candidate(3, genres: ["a", "c"]),
            Candidate(4, genres: ["b", "c", "d"]));

        // act
        var result = MmrSelector.Select(candidates, 4, 0.0, Options(genres: 1.0), UniformGenres(), Counts());

        // assert
        result.ShouldBe([new MovieId(1), new MovieId(2), new MovieId(4), new MovieId(3)]);
    }

    [Fact]
    public void Select_CapsAcrossFullRanking_SkipsBoundaryCandidatesAndKeepsStandaloneSeedSlots()
    {
        var candidates = Pool(
            new MmrCandidate(new MovieId(1), 1.0, Features(collectionId: 7), new MovieId(90)),
            new MmrCandidate(new MovieId(2), 0.9, Features(collectionId: 8), new MovieId(90)),
            new MmrCandidate(new MovieId(3), 0.8, Features(collectionId: 9), new MovieId(91)),
            new MmrCandidate(new MovieId(4), 0.7, Features(collectionId: 7), new MovieId(91)),
            new MmrCandidate(new MovieId(5), 0.6, Features()));

        var options = new RecommendationOptions { MaxPerBestSeed = 1, MaxPerCollection = 1 };
        var result = MmrSelector.Select(candidates, 5, 1.0, options, UniformGenres(), Counts());

        result.ShouldBe([new MovieId(1), new MovieId(3), new MovieId(5)]);
    }

    [Fact]
    public void Select_ActiveCaps_PreserveAscendingMovieIdTieBreak()
    {
        var candidates = Pool(
            new MmrCandidate(new MovieId(5), 1.0, Features(), new MovieId(90)),
            new MmrCandidate(new MovieId(2), 1.0, Features(), new MovieId(90)),
            new MmrCandidate(new MovieId(3), 0.5, Features(), new MovieId(91)));

        var options = new RecommendationOptions { MaxPerBestSeed = 1, MaxPerCollection = 1 };
        var result = MmrSelector.Select(candidates, 2, 1.0, options, UniformGenres(), Counts());

        result.ShouldBe([new MovieId(2), new MovieId(3)]);
    }

    [Fact]
    public void Select_CrossTakeResultsArePrefixes()
    {
        // arrange
        var candidates = Pool(
            Candidate(5, relevance: 0.5, genres: ["A"]),
            Candidate(2, relevance: 0.9, genres: ["A", "B"]),
            Candidate(7, relevance: 0.6, genres: ["C"]),
            Candidate(1, relevance: 0.8, genres: ["B"]),
            Candidate(9, relevance: 0.7, genres: ["A", "C"]));
        var weights = Options(genres: 1.0);
        var genreWeights = UniformGenres();
        var full = MmrSelector.Select(candidates, 10, 0.7, weights, genreWeights, Counts());

        // act / assert
        foreach (var take in new[] { 0, 1, 2, 3, 4 })
        {
            var shorter = MmrSelector.Select(candidates, take, 0.7, weights, genreWeights, Counts());
            shorter.ToArray().ShouldBe([.. full.Take(Math.Min(take, candidates.Count))]);
        }
    }

    private static List<MmrCandidate> Pool(params MmrCandidate[] items) => [.. items];

    private static MmrCandidate Candidate(
        int id,
        double relevance = 0.5,
        IEnumerable<string>? genres = null) =>
        new(new MovieId(id), relevance, Features(genres: genres));

    private static RecommendationOptions Options(
        double director = 0.0,
        double themes = 0.0,
        double plot = 0.0,
        double cast = 0.0,
        double genres = 0.0,
        double collection = 0.0) =>
        new()
        {
            Weights = new SimilarityWeights
            {
                Collection = collection,
                Director = director,
                Themes = themes,
                Plot = plot,
                Cast = cast,
                Genres = genres,
            },
        };

    private static KeywordCounts Counts() => new(10, new Dictionary<string, long> { ["Drama"] = 5, ["Comedy"] = 5 });

    private static GenreWeights GenreTable() =>
        GenreWeights.Calculate(
            new GenreCounts(
                100,
                new Dictionary<string, long>
                {
                    ["Drama"] = 1,
                    ["Comedy"] = 90,
                    ["Shared"] = 50,
                }));

    private static GenreWeights UniformGenres() =>
        GenreWeights.Calculate(new GenreCounts(0, new Dictionary<string, long>()));

    private static MovieFeatures Features(
        IEnumerable<string>? genres = null,
        IEnumerable<string>? directors = null,
        IEnumerable<string>? writers = null,
        IEnumerable<string>? cast = null,
        int? releaseYear = null,
        int? runtime = null,
        decimal? imdbRating = null,
        int? metaScore = null,
        string? fullPlot = null,
        int? collectionId = null) =>
        new(
            genres,
            directors,
            writers,
            cast,
            releaseYear.HasValue ? new ReleaseYear(releaseYear.Value, Now) : null,
            runtime.HasValue ? new Runtime(runtime.Value) : null,
            imdbRating.HasValue ? new ImdbRating(imdbRating.Value) : null,
            metaScore,
            fullPlot,
            collectionId: collectionId);
}
