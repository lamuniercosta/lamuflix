using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class CrowdScoringTests
{
    private const double CrowdDefaultWeight = 2.0;
    private const double SixWeightTotal = 15.0;
    private const double SevenWeightTotal = 17.0;

    private static readonly MovieId First = new(1);
    private static readonly MovieId Second = new(2);

    private static readonly GenreWeights Genres = GenreWeights.Calculate(
        new GenreCounts(
            100,
            new Dictionary<string, long>
            {
                ["Drama"] = 10,
                ["Comedy"] = 20,
            }));

    private static readonly KeywordCounts Keywords = new(0, new Dictionary<string, long>());

    private static readonly TmdbOptions ConfiguredTmdb = new()
    {
        Enabled = true,
        ApiKey = "test-key",
    };

    private static CrowdLinks LinkedPair => CrowdLinks.Create([(First, Second)]);

    public static TheoryData<bool, bool, string, bool> ResolveStates => new()
    {
        { true, true, "test-key", true },
        { true, true, "", false },
        { true, true, "   ", false },
        { true, false, "test-key", false },
        { false, true, "test-key", false },
        { false, false, "", false },
    };

    public static TheoryData<double?, bool> ThresholdEdges => new()
    {
        { null, true },
        { 0.0, true },
        { 0.5, true },
        { 1.0, true },
        { -0.0001, false },
        { 1.0001, false },
        { double.NaN, false },
        { double.PositiveInfinity, false },
        { double.NegativeInfinity, false },
    };

    public static TheoryData<bool, double?, double> StrongMatchEdges => new()
    {
        { true, 0.4, 0.4 },
        { true, null, 0.225 },
        { false, 0.4, 0.225 },
        { false, null, 0.225 },
    };

    [Fact]
    public void SimilarityWeights_Defaults_CarryCrowdWeightTwoInSeventhPlace()
    {
        // arrange
        var weights = new SimilarityWeights();

        // act
        var ordered = new[]
        {
            weights.Collection,
            weights.Director,
            weights.Themes,
            weights.Crowd,
            weights.Plot,
            weights.Cast,
            weights.Genres,
        };

        // assert
        ordered.ShouldBe([3.0, 3.0, 3.0, CrowdDefaultWeight, 2.0, 2.0, 2.0]);
    }

    [Fact]
    public void Score_ActiveCrowdOnLinkedPair_PlacesCrowdAfterThemesAndAddsItsWeightToTheDenominator()
    {
        // arrange
        var first = new MovieFeatures(genres: ["Drama"]);
        var second = new MovieFeatures(genres: ["Drama"]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            ActiveOptions(),
            Genres,
            Keywords,
            ActiveLinked(),
            First,
            Second);

        // assert
        result.Breakdown.Select(row => row.Name)
            .ShouldBe(["Collection", "Director", "Themes", "Crowd", "Plot", "Cast", "Genre"]);
        result.Breakdown.Select(row => row.Weight).ShouldBe([3.0, 3.0, 3.0, CrowdDefaultWeight, 2.0, 2.0, 2.0]);
        var crowd = result.Breakdown[3];
        crowd.Value.ShouldBe(1.0);
        crowd.Contribution.ShouldBe(CrowdDefaultWeight / SevenWeightTotal, 1e-12);
        result.Breakdown[6].Contribution.ShouldBe(2.0 / SevenWeightTotal, 1e-12);
        result.Score.ShouldBe((CrowdDefaultWeight + 2.0) / SevenWeightTotal, 1e-12);
        result.Breakdown.Sum(row => row.Contribution).ShouldBe(result.Score, 1e-12);
    }

    [Fact]
    public void Score_ActiveCrowdOnUnlinkedPair_KeepsTheCrowdWeightInTheDenominatorWithZeroValue()
    {
        // arrange
        var first = new MovieFeatures(genres: ["Drama"]);
        var second = new MovieFeatures(genres: ["Drama"]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            ActiveOptions(),
            Genres,
            Keywords,
            ActiveUnlinked(),
            First,
            Second);

        // assert
        result.Breakdown.Select(row => row.Name)
            .ShouldBe(["Collection", "Director", "Themes", "Crowd", "Plot", "Cast", "Genre"]);
        result.Breakdown[3].Value.ShouldBe(0.0);
        result.Breakdown[3].Contribution.ShouldBe(0.0);
        result.Breakdown[6].Contribution.ShouldBe(2.0 / SevenWeightTotal, 1e-12);
        result.Score.ShouldBe(2.0 / SevenWeightTotal, 1e-12);
    }

    [Fact]
    public void Score_InactiveCrowd_OmitsTheRowAndKeepsTheSixWeightDenominator()
    {
        // arrange
        var first = new MovieFeatures(genres: ["Drama"]);
        var second = new MovieFeatures(genres: ["Drama"]);
        var options = new RecommendationOptions { CrowdEnabled = true };
        var inactive = CrowdScoring.Resolve(options, new TmdbOptions { Enabled = false }, LinkedPair);

        // act
        var result = SimilarityScorer.Score(first, second, options, Genres, Keywords, inactive, First, Second);

        // assert
        result.Breakdown.Select(row => row.Name)
            .ShouldBe(["Collection", "Director", "Themes", "Plot", "Cast", "Genre"]);
        result.Breakdown[5].Contribution.ShouldBe(2.0 / SixWeightTotal, 1e-12);
        result.Score.ShouldBe(2.0 / SixWeightTotal, 1e-12);
    }

    [Fact]
    public void Score_DefaultOptions_LeavesStoredCrowdLinksUnused()
    {
        // arrange
        var first = new MovieFeatures(genres: ["Drama"]);
        var second = new MovieFeatures(genres: ["Drama"]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            new RecommendationOptions(),
            Genres,
            Keywords,
            CrowdScoring.Disabled,
            First,
            Second);

        // assert
        result.Breakdown.Count.ShouldBe(6);
        result.Breakdown.ShouldAllBe(row => row.Name != "Crowd");
        result.Score.ShouldBe(2.0 / SixWeightTotal, 1e-12);
    }

    [Theory]
    [MemberData(nameof(ResolveStates))]
    public void Resolve_RequiresBothTheActivationSwitchAndConfiguredTmdb(
        bool crowdEnabled,
        bool tmdbEnabled,
        string apiKey,
        bool expectedActive)
    {
        // arrange
        var options = new RecommendationOptions { CrowdEnabled = crowdEnabled };
        var tmdb = new TmdbOptions { Enabled = tmdbEnabled, ApiKey = apiKey };

        // act
        var crowd = CrowdScoring.Resolve(options, tmdb, LinkedPair);

        // assert
        crowd.Active.ShouldBe(expectedActive);
    }

    [Fact]
    public void IsLinked_IsSymmetricBinaryAndRejectsSelfLinks()
    {
        // arrange
        var crowd = ActiveLinked();
        var sameId = new MovieId(First.Value);

        // act / assert
        crowd.IsLinked(First, Second).ShouldBeTrue();
        crowd.IsLinked(Second, First).ShouldBeTrue();
        crowd.IsLinked(First, sameId).ShouldBeFalse();
        crowd.IsLinked(First, new MovieId(99)).ShouldBeFalse();
        CrowdScoring.Disabled.IsLinked(First, Second).ShouldBeFalse();
    }

    [Fact]
    public void BuildEvidence_ForYouCrowdWinner_QuotesTheSeedTitleAsLiteralData()
    {
        // arrange
        var result = CrowdWinningSimilarity(ActiveLinked());

        // act
        var evidence = SimilarityScorer.BuildEvidence(result, ActiveOptions(), "Seed {Title}: (Part 1)");

        // assert
        var row = evidence.ShouldHaveSingleItem();
        row.Criterion.ShouldBe("Crowd");
        row.Text.ShouldBe("Often liked together with Seed {Title}: (Part 1)");
        row.MatchedNames.ShouldBeEmpty();
    }

    [Fact]
    public void BuildEvidence_RelatedCrowdWinner_UsesTheSharedCrowdCopy()
    {
        // arrange
        var result = CrowdWinningSimilarity(ActiveLinked());

        // act
        var evidence = SimilarityScorer.BuildEvidence(result, ActiveOptions());

        // assert
        var row = evidence.ShouldHaveSingleItem();
        row.Criterion.ShouldBe("Crowd");
        row.Text.ShouldBe("Often liked together");
        row.MatchedNames.ShouldBeEmpty();
    }

    [Fact]
    public void BuildEvidence_InactiveCrowd_EmitsNoCrowdCopyEvenWhenLinksExist()
    {
        // arrange
        var first = new MovieFeatures(genres: ["Drama"]);
        var second = new MovieFeatures(genres: ["Comedy"]);
        var result = SimilarityScorer.Score(
            first,
            second,
            ActiveOptions(),
            Genres,
            Keywords,
            CrowdScoring.Resolve(ActiveOptions(), new TmdbOptions(), LinkedPair),
            First,
            Second);

        // act
        var evidence = SimilarityScorer.BuildEvidence(result, ActiveOptions(), "Seed Title");

        // assert
        evidence.ShouldBeEmpty();
    }

    [Fact]
    public void Reasons_ActiveCrowdOnLinkedPair_EmitsTheCrowdReason()
    {
        // arrange
        var result = CrowdWinningSimilarity(ActiveLinked());

        // act / assert
        result.Reasons.ShouldBe([Reason.Structural("Often liked together")]);
    }

    [Theory]
    [MemberData(nameof(StrongMatchEdges))]
    public void StrongMatchThreshold_SelectsTheCrowdActiveBarOnlyWhenActiveAndSet(
        bool crowdActive,
        double? crowdActiveThreshold,
        double expected)
    {
        // arrange
        var options = new RecommendationOptions { CrowdActiveStrongMatchThreshold = crowdActiveThreshold };

        // act
        var threshold = SimilarityScorer.StrongMatchThreshold(options, crowdActive);

        // assert
        threshold.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(ThresholdEdges))]
    public void Validate_CrowdActiveStrongMatchThreshold_AcceptsOnlyFiniteValuesInRange(
        double? threshold,
        bool expectedValid)
    {
        // arrange
        var options = new RecommendationOptions { CrowdActiveStrongMatchThreshold = threshold };

        // act
        var flagged = Validate(options)
            .Any(failure => failure.MemberNames.Contains(nameof(RecommendationOptions.CrowdActiveStrongMatchThreshold)));

        // assert
        flagged.ShouldBe(!expectedValid);
    }

    [Fact]
    public void Validate_AllZeroWeights_FailsBothModes()
    {
        // arrange
        var options = new RecommendationOptions
        {
            Weights = new SimilarityWeights { Collection = 0, Director = 0, Themes = 0, Crowd = 0, Plot = 0, Cast = 0, Genres = 0 },
        };

        // act
        var failures = Validate(options);

        // assert
        failures.ShouldContain(failure => failure.MemberNames.Contains(nameof(RecommendationOptions.Weights)));
    }

    [Fact]
    public void Validate_InfiniteCrowdActiveSum_FailsEvenWithCrowdInactive()
    {
        // arrange
        var options = new RecommendationOptions
        {
            Weights = new SimilarityWeights { Crowd = double.MaxValue, Themes = double.MaxValue, Director = 0, Plot = 0, Cast = 0, Genres = 0, Collection = 0 },
        };

        // act
        var failures = Validate(options);

        // assert
        failures.ShouldContain(failure => failure.MemberNames.Contains(nameof(RecommendationOptions.Weights)));
    }

    [Fact]
    public void Defaults_CrowdActivationAndThreshold_AreOffAndUnset()
    {
        // arrange
        var options = new RecommendationOptions();

        // act / assert
        options.CrowdEnabled.ShouldBeFalse();
        options.CrowdActiveStrongMatchThreshold.ShouldBeNull();
        Validate(options).ShouldBeEmpty();
    }

    private static SimilarityResult CrowdWinningSimilarity(CrowdScoring crowd) =>
        SimilarityScorer.Score(
            new MovieFeatures(genres: ["Drama"]),
            new MovieFeatures(genres: ["Comedy"]),
            ActiveOptions(),
            Genres,
            Keywords,
            crowd,
            First,
            Second);

    private static RecommendationOptions ActiveOptions() => new() { CrowdEnabled = true };

    private static CrowdScoring ActiveLinked() => CrowdScoring.Resolve(ActiveOptions(), ConfiguredTmdb, LinkedPair);

    private static CrowdScoring ActiveUnlinked() => CrowdScoring.Resolve(ActiveOptions(), ConfiguredTmdb, CrowdLinks.Empty);

    private static IReadOnlyList<ValidationResult> Validate(RecommendationOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
