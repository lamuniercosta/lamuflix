using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityResultBreakdownTests
{
    private const double Tolerance = 1e-12;
    private static readonly string[] OrderedCriterionNames = ["Collection", "Director", "Themes", "Plot", "Cast", "Genre"];

    [Fact]
    public void Score_EveryResult_CarriesTheSixOrderedCriteria()
    {
        // arrange
        var first = Features(
            genres: ["Drama"],
            directors: ["Alpha"],
            cast: ["Actor A", "Actor B"],
            keywordNames: ["theme"],
            collectionId: 7,
            collectionName: "Saga",
            plotEmbedding: [1f, 0f]);
        var second = Features(
            genres: ["Drama"],
            directors: ["Alpha"],
            cast: ["Actor A", "Actor B"],
            keywordNames: ["theme"],
            collectionId: 7,
            collectionName: "Saga",
            plotEmbedding: [1f, 0f]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), Keywords(10, ("theme", 2)));

        // assert
        result.Breakdown.Select(entry => entry.Name).ShouldBe(OrderedCriterionNames);
        result.Breakdown.Select(entry => entry.Weight).ShouldBe([3.0, 3.0, 3.0, 2.0, 2.0, 2.0]);
        result.Breakdown.Select(entry => entry.Value).ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
        result.Breakdown.Select(entry => entry.Contribution).ShouldBe(
            [3.0 / 15.0, 3.0 / 15.0, 3.0 / 15.0, 2.0 / 15.0, 2.0 / 15.0, 2.0 / 15.0]);
    }

    [Fact]
    public void Score_CriterionEntry_ExposesNameValueWeightEvidenceAndContribution()
    {
        // arrange
        var first = Features(collectionId: 7, collectionName: "Saga");
        var second = Features(collectionId: 7, collectionName: "Saga");

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        var entry = result.Breakdown[0];
        entry.Name.ShouldBe("Collection");
        entry.Value.ShouldBe(1.0);
        entry.Weight.ShouldBe(3.0);
        entry.Evidence.ShouldBe(["Saga"]);
        entry.Contribution.ShouldBe(3.0 / 15.0, Tolerance);
    }

    [Fact]
    public void Score_ConfiguredWeights_AreReportedUnchangedOnEachEntry()
    {
        // arrange
        var weights = new SimilarityWeights
        {
            Collection = 1.0,
            Director = 2.0,
            Themes = 4.0,
            Plot = 5.0,
            Cast = 6.0,
            Genres = 7.0,
        };
        var options = new RecommendationOptions { Weights = weights };
        var first = Features(directors: ["Alpha"]);
        var second = Features(directors: ["alpha"]);

        // act
        var result = SimilarityScorer.Score(first, second, options, GenreTable(), NoKeywords());

        // assert
        result.Breakdown.Select(entry => entry.Weight).ShouldBe([1.0, 2.0, 4.0, 5.0, 6.0, 7.0]);
        result.Breakdown[1].Contribution.ShouldBe(2.0 / 25.0, Tolerance);
    }

    [Fact]
    public void Score_MissingData_KeepsSixZeroRowsInTheDefaultDenominator()
    {
        // arrange
        var empty = Features();

        // act
        var result = SimilarityScorer.Score(empty, empty, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown.Select(entry => entry.Name).ShouldBe(OrderedCriterionNames);
        result.Breakdown.Count.ShouldBe(6);
        result.Breakdown.ShouldAllBe(entry => entry.Value == 0.0 && entry.Contribution == 0.0);
        result.Breakdown.ShouldAllBe(entry => entry.Evidence.IsEmpty);
        result.Score.ShouldBe(0.0);
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_PartialMatch_KeepsUnmatchedCriteriaAsZeroRows()
    {
        // arrange
        var first = Features(collectionId: 7, collectionName: "Saga");
        var second = Features(collectionId: 7, collectionName: "Saga");

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown.Select(entry => entry.Name).ShouldBe(OrderedCriterionNames);
        result.Breakdown.Skip(1).ShouldAllBe(entry => entry.Value == 0.0 && entry.Contribution == 0.0);
        result.Score.ShouldBe(3.0 / 15.0, Tolerance);
    }

    [Fact]
    public void Score_AllZeroScoringWeights_IsRejected()
    {
        // arrange
        var options = new RecommendationOptions
        {
            Weights = new SimilarityWeights { Collection = 0.0, Director = 0.0, Themes = 0.0, Plot = 0.0, Cast = 0.0, Genres = 0.0 },
        };

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() =>
            SimilarityScorer.Score(Features(), Features(), options, GenreTable(), NoKeywords()));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Score_Contributions_SumToScore()
    {
        // arrange
        var first = Features(
            genres: ["Drama", "Comedy"],
            directors: ["Alpha", "Beta"],
            cast: ["Delta"],
            keywordNames: ["theme", "Other"],
            collectionId: 7,
            plotEmbedding: [1f, 0f]);
        var second = Features(
            genres: ["Drama", "Shared"],
            directors: ["Alpha"],
            cast: ["Delta", "Epsilon"],
            keywordNames: ["theme"],
            collectionId: 7,
            plotEmbedding: [0.6f, 0.8f]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), Keywords(100, ("theme", 5), ("Other", 20)));

        // assert
        result.Breakdown.Sum(entry => entry.Contribution).ShouldBe(result.Score, Tolerance);
        result.Score.ShouldBeGreaterThan(0.0);
        result.Score.ShouldBeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void Score_Reasons_AreStructuralAndInTableOrder()
    {
        // arrange
        var first = Features(directors: ["Alpha"], cast: ["Actor"], genres: ["Drama"]);
        var second = Features(directors: ["Alpha"], cast: ["Actor"], genres: ["Drama"]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Reasons.ShouldAllBe(reason => reason.Kind == ReasonKind.Structural);
        result.Reasons.Select(reason => reason.Text).ShouldBe(
            ["Same director: Alpha", "Shares cast: Actor", "Same genres: Drama"]);
    }

    [Fact]
    public void Score_TwoArgumentConstruction_LeavesBreakdownEmpty()
    {
        // arrange
        var result = new SimilarityResult(0.5, [Reason.Structural("Shared themes: theme")]);

        // assert
        result.Breakdown.ShouldBeEmpty();
        result.Score.ShouldBe(0.5);
        result.Reasons.ShouldHaveSingleItem().ShouldBe(Reason.Structural("Shared themes: theme"));
    }

    [Fact]
    public void Score_TwoArgumentDeconstruction_DeconstructsScoreAndReasonsOnly()
    {
        // arrange
        var reasons = ImmutableList.Create(Reason.Structural("Shared themes: theme"));

        // act
        var (score, decomposed) = new SimilarityResult(0.5, reasons);

        // assert
        score.ShouldBe(0.5);
        decomposed.ShouldBe(reasons);
    }

    [Fact]
    public void Score_BlendStyleConstruction_KeepsBreakdownEmpty()
    {
        // arrange
        var structural = SimilarityScorer.Score(
            Features(directors: ["Alpha"]),
            Features(directors: ["Alpha"]),
            Options(),
            GenreTable(),
            NoKeywords());
        structural.Breakdown.Count.ShouldBe(6);

        // act
        var blended = new SimilarityResult(
            structural.Score,
            structural.Reasons.Add(Reason.Personal("Because you liked similar films")));

        // assert
        blended.Breakdown.ShouldBeEmpty();
        blended.Reasons.Count.ShouldBe(2);
    }

    [FsCheck.Xunit.Property(MaxTest = 200, Replay = "545001,100003")]
    [Trait("Category", "Property")]
    public bool Score_GeneratedPairs_KeepSixFiniteNonNegativeFieldsWithContributionsSummingToScore(int seed)
    {
        // arrange
        var first = Generated(seed);
        var second = Generated(unchecked((seed * 31) + 7919));
        var counts = Keywords(64, ("theme", 4), ("motif", 13), ("sequel", 2));

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);

        // assert
        var canonical = result.Breakdown.Count == 6
            && result.Breakdown.Select(entry => entry.Name).SequenceEqual(OrderedCriterionNames)
            && result.Breakdown.All(entry =>
                entry.Value >= 0.0
                && entry.Value <= 1.0
                && entry.Weight >= 0.0
                && double.IsFinite(entry.Contribution))
            && result.Breakdown.Any(entry => entry.Weight > 0.0)
            && double.IsFinite(result.Score)
            && Math.Abs(result.Breakdown.Sum(entry => entry.Contribution) - result.Score) <= 1e-9;
        return canonical;
    }

    private static MovieFeatures Generated(int seed)
    {
        var positive = Math.Abs(seed);
        return new MovieFeatures(
            genres: (positive % 3) switch
            {
                0 => ["Drama"],
                1 => ["Drama", "Comedy"],
                _ => [],
            },
            directors: (positive % 4) is 0 or 1 ? ["Alpha", "Beta"] : [],
            cast: (positive % 7) == 0 ? ["Delta"] : [],
            keywordNames: (positive % 5) switch
            {
                0 => ["theme"],
                1 => ["theme", "motif"],
                2 => ["sequel"],
                _ => [],
            },
            collectionId: (positive % 6) == 0 ? 7 : null,
            collectionName: (positive % 6) == 0 ? "Saga" : null,
            plotEmbedding: (positive % 3) == 0 ? [1f, 0f] : null);
    }

    private static RecommendationOptions Options() => new();

    private static KeywordCounts NoKeywords() => new(0, new Dictionary<string, long>());

    private static KeywordCounts Keywords(long totalMovies, params (string Name, long Count)[] entries) =>
        new(totalMovies, entries.ToDictionary(entry => entry.Name, entry => entry.Count, StringComparer.Ordinal));

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

    private static MovieFeatures Features(
        IEnumerable<string>? genres = null,
        IEnumerable<string>? directors = null,
        IEnumerable<string>? cast = null,
        IEnumerable<string>? keywordNames = null,
        int? collectionId = null,
        string? collectionName = null,
        IEnumerable<float>? plotEmbedding = null) =>
        new(
            genres: genres,
            directors: directors,
            cast: cast,
            collectionId: collectionId,
            keywordNames: keywordNames,
            collectionName: collectionName,
            plotEmbedding: plotEmbedding);
}
