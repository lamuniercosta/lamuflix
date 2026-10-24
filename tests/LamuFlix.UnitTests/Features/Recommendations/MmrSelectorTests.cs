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
        var failure = Should.Throw<ArgumentNullException>(() => MmrSelector.Select(null!, 1, 0.7, Only(), GenreTable()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_NullWeights_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => MmrSelector.Select(Pool(Candidate(1)), 1, 0.7, null!, GenreTable()));

        // assert
        failure.ParamName.ShouldBe("weights");
    }

    [Fact]
    public void Select_NullGenreWeights_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => MmrSelector.Select(Pool(Candidate(1)), 1, 0.7, Only(), null!));

        // assert
        failure.ParamName.ShouldBe("genreWeights");
    }

    [Fact]
    public void Select_NullCandidateItem_ThrowsArgumentException()
    {
        // arrange
        var candidates = new List<MmrCandidate> { Candidate(1), null! };

        // act
        var failure = Should.Throw<ArgumentException>(() => MmrSelector.Select(candidates, 1, 0.7, Only(), GenreTable()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_NullCandidateMovieId_ThrowsArgumentException()
    {
        // arrange
        var candidates = Pool(new MmrCandidate(null!, 0.5, Features()));

        // act
        var failure = Should.Throw<ArgumentException>(() => MmrSelector.Select(candidates, 1, 0.7, Only(), GenreTable()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_NullCandidateFeatures_ThrowsArgumentException()
    {
        // arrange
        var candidates = Pool(new MmrCandidate(new MovieId(1), 0.5, null!));

        // act
        var failure = Should.Throw<ArgumentException>(() => MmrSelector.Select(candidates, 1, 0.7, Only(), GenreTable()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_DuplicateMovieIds_ThrowsArgumentException()
    {
        // arrange
        var candidates = Pool(Candidate(7), Candidate(7, relevance: 0.4));

        // act
        var failure = Should.Throw<ArgumentException>(() => MmrSelector.Select(candidates, 2, 0.7, Only(), GenreTable()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_NegativeTake_ThrowsArgumentOutOfRangeException()
    {
        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(Pool(Candidate(1)), -1, 0.7, Only(), GenreTable()));

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
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(Pool(Candidate(1)), 1, lambda, Only(), GenreTable()));

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
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(candidates, 1, 0.7, Only(), GenreTable()));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Select_InvalidWeightOnAnyOfSevenFields_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        SimilarityWeights[] weights =
        [
            new() { Director = -1.0 },
            new() { Writers = double.NaN },
            new() { Cast = double.PositiveInfinity },
            new() { Genres = double.NegativeInfinity },
            new() { YearProximity = -0.25 },
            new() { RuntimeProximity = double.NaN },
            new() { RatingBoost = double.PositiveInfinity },
        ];

        // act / assert
        foreach (var invalid in weights)
        {
            var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(Pool(Candidate(1)), 1, 0.7, invalid, GenreTable()));
            failure.ParamName.ShouldBe("weights");
        }
    }

    [Fact]
    public void Select_InvalidWeightsWithValidEmptyPool_StillThrows()
    {
        // arrange
        var noCandidates = new List<MmrCandidate>();
        var weights = new SimilarityWeights { Genres = -1.0 };

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(noCandidates, 1, 0.7, weights, GenreTable()));

        // assert
        failure.ParamName.ShouldBe("weights");
    }

    [Fact]
    public void Select_InvalidWeightsWithZeroTake_StillThrows()
    {
        // arrange
        var weights = new SimilarityWeights { Director = double.NaN };

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => MmrSelector.Select(Pool(Candidate(1)), 0, 0.7, weights, GenreTable()));

        // assert
        failure.ParamName.ShouldBe("weights");
    }

    [Fact]
    public void Select_ValidEmptyPool_ReturnsEmpty()
    {
        // arrange
        var noCandidates = new List<MmrCandidate>();

        // act
        var result = MmrSelector.Select(noCandidates, 5, 0.7, Only(), GenreTable());

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Select_ZeroTake_ReturnsEmpty()
    {
        // act
        var result = MmrSelector.Select(Pool(Candidate(1), Candidate(2)), 0, 0.7, Only(), GenreTable());

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
        var result = MmrSelector.Select(candidates, 10, 0.7, Only(), GenreTable());

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
        var result = MmrSelector.Select(candidates, 1, 0.7, Only(), GenreTable());

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
        MmrSelector.Select(candidates, 2, 0.7, Only(genres: 1.0), GenreTable());

        // assert
        candidates.ShouldBe(before);
    }

    [Fact]
    public void Select_LeavesFeaturesAndWeightsUnchanged()
    {
        // arrange
        var features = Features(genres: ["Drama"]);
        var candidates = Pool(new MmrCandidate(new MovieId(1), 0.5, features));
        var weights = Only(genres: 1.0);
        var genreWeights = GenreTable();
        var dramaWeight = genreWeights.GetWeight("Drama");

        // act
        MmrSelector.Select(candidates, 1, 0.7, weights, genreWeights);

        // assert
        candidates[0].Features.ShouldBeSameAs(features);
        candidates[0].Features.ShouldBe(features);
        weights.ShouldBe(Only(genres: 1.0));
        genreWeights.GetWeight("Drama").ShouldBe(dramaWeight);
    }

    [Fact]
    public void Select_InterleavedCallsWithDifferentLambdaAndTake_RetainsEarlierResult()
    {
        // arrange
        var candidates = Pool(Candidate(1, 0.9), Candidate(2, 0.8), Candidate(3, 0.7));
        var first = MmrSelector.Select(candidates, 2, 0.7, Only(), GenreTable());
        var firstSnapshot = first.ToArray();

        // act
        var second = MmrSelector.Select(candidates, 1, 0.2, Only(), GenreTable());
        var repeated = MmrSelector.Select(candidates, 2, 0.7, Only(), GenreTable());

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
        var result = MmrSelector.Select(candidates, 4, 1.0, Only(genres: 1.0), GenreTable());

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
        var result = MmrSelector.Select(candidates, 4, 0.0, Only(genres: 1.0), UniformGenres());

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
            var result = MmrSelector.Select(candidates, 4, 0.7, Only(genres: 1.0), UniformGenres());
            result.ShouldBe(expected);
        }
    }

    [Fact]
    public void Select_ZeroWeights_ReusesDevice338IdentitySimilarity()
    {
        // arrange
        var candidates = Pool(
            Candidate(1, genres: ["A"]),
            Candidate(2, genres: ["A"]),
            Candidate(3, genres: ["B"]));

        // act
        var result = MmrSelector.Select(candidates, 3, 0.0, Only(), UniformGenres());

        // assert
        result.ShouldBe([new MovieId(1), new MovieId(3), new MovieId(2)]);
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
        var result = MmrSelector.Select(candidates, 3, 0.0, Only(genres: 1.0), UniformGenres());

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
        var result = MmrSelector.Select(candidates, 4, 0.0, Only(genres: 1.0), UniformGenres());

        // assert
        // Latest-only redundancy would pick id 3 at step three, because it is less similar to id 2.
        // The cumulative maximum retains the earlier id-1 similarity and picks id 4.
        result.ShouldBe([new MovieId(1), new MovieId(2), new MovieId(4), new MovieId(3)]);
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
        var weights = Only(genres: 1.0);
        var genreWeights = UniformGenres();
        var full = MmrSelector.Select(candidates, 10, 0.7, weights, genreWeights);

        // act / assert
        foreach (var take in new[] { 0, 1, 2, 3, 4 })
        {
            var shorter = MmrSelector.Select(candidates, take, 0.7, weights, genreWeights);
            shorter.ToArray().ShouldBe([.. full.Take(Math.Min(take, candidates.Count))]);
        }
    }

    private static List<MmrCandidate> Pool(params MmrCandidate[] items) => [.. items];

    private static MmrCandidate Candidate(
        int id,
        double relevance = 0.5,
        IEnumerable<string>? genres = null) =>
        new(new MovieId(id), relevance, Features(genres: genres));

    private static SimilarityWeights Only(
        double director = 0.0,
        double writers = 0.0,
        double cast = 0.0,
        double genres = 0.0,
        double year = 0.0,
        double runtime = 0.0,
        double rating = 0.0) =>
        new()
        {
            Director = director,
            Writers = writers,
            Cast = cast,
            Genres = genres,
            YearProximity = year,
            RuntimeProximity = runtime,
            RatingBoost = rating,
        };

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
        string? fullPlot = null) =>
        new(
            genres,
            directors,
            writers,
            cast,
            releaseYear.HasValue ? new ReleaseYear(releaseYear.Value, Now) : null,
            runtime.HasValue ? new Runtime(runtime.Value) : null,
            imdbRating.HasValue ? new ImdbRating(imdbRating.Value) : null,
            metaScore,
            fullPlot);
}
