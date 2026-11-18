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
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] WeightedCriterionNames = ["Collection", "Director", "Themes"];

    [Fact]
    public void Score_WeightedCriteria_ContributionsSumToScore()
    {
        // arrange
        var first = Features(
            directors: ["Alpha", "Beta"],
            writers: ["Gamma"],
            cast: ["Delta", "Epsilon"],
            genres: ["Drama", "Comedy"],
            releaseYear: 2001,
            runtime: 104,
            imdbRating: 7.8m,
            keywords: [11, 22, 33],
            collectionId: 7);
        var second = Features(
            directors: ["Alpha"],
            writers: ["Gamma", "Zeta"],
            cast: ["Delta"],
            genres: ["Drama", "Shared"],
            releaseYear: 2004,
            runtime: 118,
            imdbRating: 7.1m,
            keywords: [22, 44],
            collectionId: 7);

        // act
        var result = SimilarityScorer.Score(first, second, new SimilarityWeights(), GenreTable());

        // assert
        result.Breakdown.ShouldNotBeEmpty();
        result.Breakdown.Sum(entry => entry.Contribution).ShouldBe(result.Score, Tolerance);
    }

    [Fact]
    public void Score_WeightedCriteria_EmitsExpectedCriterionNamesInCollectionOrder()
    {
        // arrange
        var first = Features(directors: ["Alpha"], genres: ["Drama"], releaseYear: 2001, keywords: [11], collectionId: 7);
        var second = Features(directors: ["Alpha"], genres: ["Drama"], releaseYear: 2003, keywords: [11], collectionId: 7);

        // act
        var result = SimilarityScorer.Score(first, second, new SimilarityWeights(), GenreTable());

        // assert
        result.Breakdown.Select(entry => entry.Name).ShouldBe(
        [
            "Director",
            "Keyword",
            "Collection",
            "Genres",
            "YearProximity",
        ]);
    }

    [Fact]
    public void Score_ActiveWeightedCriterion_UsesWeightTimesValueOverDenominator()
    {
        // arrange
        var first = Features(directors: ["Alpha"]);
        var second = Features(directors: ["Alpha"]);
        var weights = Only(director: 2.0, year: 0.0);

        // act
        var result = SimilarityScorer.Score(first, second, weights, GenreTable());

        // assert
        var entry = result.Breakdown.ShouldHaveSingleItem();
        entry.Name.ShouldBe("Director");
        entry.Value.ShouldBe(1.0);
        entry.Weight.ShouldBe(2.0);
        entry.Contribution.ShouldBe(1.0, Tolerance);
        result.Score.ShouldBe(1.0);
    }

    [Fact]
    public void Score_InactiveCriterion_ContributesZeroButKeepsValue()
    {
        // arrange
        var first = Features(keywords: [11, 22], collectionId: 7);
        var second = Features(keywords: [11], collectionId: 9);

        // act
        var result = SimilarityScorer.Score(first, second, Only(keyword: 0.0, collection: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(0.0);
        result.Breakdown.Select(entry => entry.Name).ShouldBe(["Keyword", "Collection"]);

        var keyword = result.Breakdown[0];
        keyword.Value.ShouldBe(0.5, Tolerance);
        keyword.Weight.ShouldBe(0.0);
        keyword.Contribution.ShouldBe(0.0);

        var collection = result.Breakdown[1];
        collection.Value.ShouldBe(0.0);
        collection.Weight.ShouldBe(1.0);
        collection.Contribution.ShouldBe(0.0);
    }

    [Fact]
    public void Score_UncollectedCriteria_AreOmittedFromBreakdown()
    {
        // arrange
        var first = Features(collectionId: 7);
        var second = Features(collectionId: 7);

        // act
        var result = SimilarityScorer.Score(first, second, new SimilarityWeights(), GenreTable());

        // assert
        result.Breakdown.Select(entry => entry.Name).ShouldBe(["Collection"]);
    }

    [Fact]
    public void Score_IdenticalProfileZeroWeight_EmitsIdentityFallbackDiagnostic()
    {
        // arrange
        var profile = Features(directors: ["Alpha"], genres: ["Drama"], releaseYear: 2001);

        // act
        var result = SimilarityScorer.Score(profile, profile, Only(), GenreTable());

        // assert
        result.Score.ShouldBe(1.0);

        var fallback = result.Breakdown.Single(entry => entry.Name == "IdentityFallback");
        fallback.Value.ShouldBe(1.0);
        fallback.Weight.ShouldBe(0.0);
        fallback.Contribution.ShouldBe(1.0);
        WeightedCriterionNames.ShouldNotContain(fallback.Name);
        result.Breakdown.Sum(entry => entry.Contribution).ShouldBe(result.Score, Tolerance);
    }

    [Fact]
    public void Score_IdenticalProfileZeroWeight_KeepsWeightedTermsAtZeroContribution()
    {
        // arrange
        var profile = Features(directors: ["Alpha"], collectionId: 7);

        // act
        var result = SimilarityScorer.Score(profile, profile, Only(), GenreTable());

        // assert
        result.Breakdown.Select(entry => entry.Name).ShouldBe(["Director", "Collection", "IdentityFallback"]);
        result.Breakdown.Take(2).ShouldAllBe(entry => entry.Contribution.Equals(0.0));
        result.Breakdown.Sum(entry => entry.Contribution).ShouldBe(result.Score, Tolerance);
    }

    [Fact]
    public void Score_NonIdentityZeroDenominator_ContributesZeroWithoutIdentityFallback()
    {
        // arrange
        var first = Features(directors: ["Alpha"], collectionId: 7);
        var second = Features(directors: ["Beta"], collectionId: 9);

        // act
        var result = SimilarityScorer.Score(first, second, Only(), GenreTable());

        // assert
        result.Score.ShouldBe(0.0);
        result.Breakdown.ShouldNotBeEmpty();
        result.Breakdown.ShouldNotContain(entry => entry.Name == "IdentityFallback");
        result.Breakdown.ShouldAllBe(entry => entry.Contribution.Equals(0.0));
        result.Breakdown.Sum(entry => entry.Contribution).ShouldBe(result.Score, Tolerance);
    }

    [Fact]
    public void Score_TwoArgumentConstruction_LeavesBreakdownEmpty()
    {
        // arrange
        var result = new SimilarityResult(0.5, [Reason.Structural("Shared keywords")]);

        // assert
        result.Breakdown.ShouldBeEmpty();
        result.Score.ShouldBe(0.5);
        result.Reasons.ShouldHaveSingleItem().ShouldBe(Reason.Structural("Shared keywords"));
    }

    [Fact]
    public void Score_TwoArgumentDeconstruction_DeconstructsScoreAndReasonsOnly()
    {
        // arrange
        var reasons = ImmutableList.Create(Reason.Structural("Shared keywords"));

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
            Only(director: 1.0),
            GenreTable());
        structural.Breakdown.ShouldNotBeEmpty();

        // act
        var blended = new SimilarityResult(
            structural.Score,
            structural.Reasons.Add(Reason.Personal("Because you liked similar films")));

        // assert
        blended.Breakdown.ShouldBeEmpty();
        blended.Reasons.Count.ShouldBe(2);
    }

    [Fact]
    public void Score_WeightedCriteria_ReasonKindAndTextSequenceUnchanged()
    {
        // arrange
        var first = Features(directors: ["Alpha"], keywords: [11], collectionId: 7, releaseYear: 2001);
        var second = Features(directors: ["Alpha"], keywords: [11], collectionId: 7, releaseYear: 2003);

        // act
        var result = SimilarityScorer.Score(first, second, new SimilarityWeights(), GenreTable());

        // assert
        result.Reasons.Select(reason => reason.Kind).ShouldAllBe(kind => kind == ReasonKind.Structural);
        result.Reasons.Select(reason => reason.Text).ShouldBe(
        [
            "Same director: Alpha",
            "Shared keywords",
            "Same franchise collection",
            "Similar release years",
        ]);
    }

    [Fact]
    public void Score_IdentityFallbackBranch_KeepsIdentityReasonUnchanged()
    {
        // arrange
        var profile = Features(directors: ["Alpha"]);

        // act
        var result = SimilarityScorer.Score(profile, profile, Only(), GenreTable());

        // assert
        var reason = result.Reasons.ShouldHaveSingleItem();
        reason.Kind.ShouldBe(ReasonKind.Structural);
        reason.Text.ShouldBe("Identical feature profile");
    }

    [Fact]
    public void Score_UncollectedOnlyWithZeroWeights_ProducesEmptyZeroBreakdown()
    {
        // arrange
        var empty = new MovieFeatures();

        // act
        var result = SimilarityScorer.Score(empty, empty, Only(), GenreTable());

        // assert
        result.Score.ShouldBe(0.0);
        result.Breakdown.ShouldBeEmpty();
        result.Reasons.ShouldBeEmpty();
    }

    [FsCheck.Xunit.Property(MaxTest = 200, Replay = "545001,100003")]
    [Trait("Category", "Property")]
    public bool Score_GeneratedPairs_BreakdownContributionsSumToScore(int seed)
    {
        // arrange
        var first = Generated(seed);
        var second = Generated(unchecked(seed * 31 + 7919));
        var weights = GeneratedWeights(seed);

        // act
        var result = SimilarityScorer.Score(first, second, weights, GenreTable());

        // assert
        return Math.Abs(result.Breakdown.Sum(entry => entry.Contribution) - result.Score) <= 1e-9;
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
            writers: (positive % 5) is 0 or 1 ? ["Gamma"] : [],
            cast: (positive % 7) == 0 ? ["Delta"] : [],
            releaseYear: (positive % 2) == 0 ? new ReleaseYear(1990 + positive % 40, Now) : null,
            runtime: (positive % 3) == 0 ? new Runtime(90 + positive % 60) : null,
            imdbRating: (positive % 4) == 0 ? new ImdbRating((positive % 100) / 10.0m) : null,
            keywords: (positive % 3) == 1 ? [11, 22] : [],
            collectionId: (positive % 6) == 0 ? 7 : null);
    }

    private static SimilarityWeights GeneratedWeights(int seed) =>
        (Math.Abs(seed) % 4) switch
        {
            0 => Only(),
            1 => Only(director: 1.0, genres: 2.0),
            2 => new SimilarityWeights(),
            _ => Only(director: 1.0, genres: 1.0, year: 1.0, keyword: 1.0, collection: 1.0),
        };

    private static SimilarityWeights Only(
        double director = 0.0,
        double genres = 0.0,
        double year = 0.0,
        double keyword = 0.0,
        double collection = 0.0) =>
        new()
        {
            Director = director,
            Writers = 0.0,
            Cast = 0.0,
            Genres = genres,
            YearProximity = year,
            RuntimeProximity = 0.0,
            RatingBoost = 0.0,
            Keyword = keyword,
            Collection = collection,
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

    private static MovieFeatures Features(
        IEnumerable<string>? genres = null,
        IEnumerable<string>? directors = null,
        IEnumerable<string>? writers = null,
        IEnumerable<string>? cast = null,
        int? releaseYear = null,
        int? runtime = null,
        decimal? imdbRating = null,
        IEnumerable<int>? keywords = null,
        int? collectionId = null) =>
        new(
            genres,
            directors,
            writers,
            cast,
            releaseYear.HasValue ? new ReleaseYear(releaseYear.Value, Now) : null,
            runtime.HasValue ? new Runtime(runtime.Value) : null,
            imdbRating.HasValue ? new ImdbRating(imdbRating.Value) : null,
            null,
            null,
            keywords,
            collectionId);
}
