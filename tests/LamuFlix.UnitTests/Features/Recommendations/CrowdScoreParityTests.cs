using System.Linq;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

/// <summary>
/// Parity pins for the states where Crowd must be absent: TMDb disabled, and TMDb enabled with a
/// blank API key. Expected values were captured from revision df855da3, the last revision carrying
/// the pre-Crowd scorer, using the identical fixture inputs in <see cref="CrowdParityFixture"/>.
/// They are never derived from the changed scorer.
/// </summary>
public sealed class CrowdScoreParityTests
{
    private const double ForYouPreChangeScore = 0.5358206122133712;

    private const double RelatedPreChangeScore = 0.5358206122133712;

    public static TheoryData<string, TmdbOptions> UnconfiguredTmdb => new()
    {
        { "disabled", new TmdbOptions { Enabled = false, ApiKey = "test-key" } },
        { "blank-key", new TmdbOptions { Enabled = true, ApiKey = "" } },
        { "whitespace-key", new TmdbOptions { Enabled = true, ApiKey = "   " } },
    };

    [Theory]
    [MemberData(nameof(UnconfiguredTmdb))]
    public void ForYou_CrowdEnabledButTmdbUnconfigured_KeepsPreChangeScoreAndCriteriaTable(
        string state,
        TmdbOptions tmdb)
    {
        // arrange
        _ = state;
        var options = new RecommendationOptions { CrowdEnabled = true };
        var crowd = CrowdScoring.Resolve(options, tmdb, CrowdParityFixture.Links);

        // act
        var match = CrowdParityFixture.ForYouWith(crowd);

        // assert
        crowd.Active.ShouldBeFalse();
        match.Score.ShouldBe(ForYouPreChangeScore);
        match.Pair.Breakdown.Select(row => row.Name)
            .ShouldBe(["Collection", "Director", "Themes", "Plot", "Cast", "Genre"]);
    }

    [Theory]
    [MemberData(nameof(UnconfiguredTmdb))]
    public void Related_CrowdEnabledButTmdbUnconfigured_KeepsPreChangeScoreAndCriteriaTable(
        string state,
        TmdbOptions tmdb)
    {
        // arrange
        _ = state;
        var options = new RecommendationOptions { CrowdEnabled = true };
        var crowd = CrowdScoring.Resolve(options, tmdb, CrowdParityFixture.Links);

        // act
        var result = CrowdParityFixture.RelatedWith(crowd);

        // assert
        crowd.Active.ShouldBeFalse();
        result.Score.ShouldBe(RelatedPreChangeScore);
        result.Breakdown.Select(row => row.Name)
            .ShouldBe(["Collection", "Director", "Themes", "Plot", "Cast", "Genre"]);
        result.Breakdown.Select(row => row.Contribution).ShouldBe(
            [0.0, 0.2, 0.0, 0.12837247925306366, 0.13333333333333333, 0.07411479962697422]);
    }

    [Fact]
    public void Related_DefaultOptions_MatchThePreChangeScore()
    {
        // arrange / act
        var result = CrowdParityFixture.Related();

        // assert
        result.Score.ShouldBe(RelatedPreChangeScore);
    }
}
