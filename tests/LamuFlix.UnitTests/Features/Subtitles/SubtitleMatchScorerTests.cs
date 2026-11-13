using LamuFlix.Core.Features.Subtitles;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleMatchScorerTests
{
    private const string VideoStem = "Dune.2021.1080p.BluRay.x264-GROUP";

    [Fact]
    public void Score_MovieHashMatch_ReturnsOneHundredRegardlessOfNames()
    {
        // act
        var score = SubtitleMatchScorer.Score(VideoStem, "Completely.Unrelated.Release", matchesMovieHash: true);

        // assert
        score.ShouldBe(100);
    }

    [Fact]
    public void Score_IdenticalReleaseAndStem_ReturnsOneHundred()
    {
        // act
        var score = SubtitleMatchScorer.Score(VideoStem, VideoStem, matchesMovieHash: false);

        // assert
        score.ShouldBe(100);
    }

    [Fact]
    public void Score_NoSharedTokens_ReturnsZero()
    {
        // act
        var score = SubtitleMatchScorer.Score("Heat.1995.1080p", "Alien.1979.WEB-DL", matchesMovieHash: false);

        // assert
        score.ShouldBe(0);
    }

    [Fact]
    public void Score_EmptyUnion_ReturnsZero()
    {
        // act
        var score = SubtitleMatchScorer.Score("...", "---", matchesMovieHash: false);

        // assert
        score.ShouldBe(0);
    }

    [Fact]
    public void Score_NullInputsWithoutHash_ReturnsZero()
    {
        // act
        var score = SubtitleMatchScorer.Score(null, null, matchesMovieHash: false);

        // assert
        score.ShouldBe(0);
    }

    [Fact]
    public void Score_HalfOverlap_RoundsMidpointAwayFromZero()
    {
        // arrange
        var video = "alpha beta gamma delta";

        // act
        var score = SubtitleMatchScorer.Score(video, "alpha beta", matchesMovieHash: false);

        // assert
        score.ShouldBe(50);
    }

    [Fact]
    public void Score_OneOfThreeTokens_RoundsThirtyThree()
    {
        // act
        var score = SubtitleMatchScorer.Score("alpha beta gamma", "alpha", matchesMovieHash: false);

        // assert
        score.ShouldBe(33);
    }

    [Fact]
    public void Score_TwoOfThreeTokens_RoundsSixtySeven()
    {
        // act
        var score = SubtitleMatchScorer.Score("alpha beta gamma", "alpha beta", matchesMovieHash: false);

        // assert
        score.ShouldBe(67);
    }

    [Fact]
    public void DistinctTokens_RepeatedTokens_AreDeduplicated()
    {
        // act
        var tokens = SubtitleMatchScorer.DistinctTokens("Dune DUNE dune 2021");

        // assert
        tokens.ShouldBe(["dune", "2021"], ignoreOrder: true);
    }

    [Fact]
    public void DistinctTokens_CaseAndPunctuationFolding_ProducesLowercaseAlphanumericTokens()
    {
        // act
        var tokens = SubtitleMatchScorer.DistinctTokens("Dune.2021_1080p-BluRay (x264)");

        // assert
        tokens.ShouldBe(["dune", "2021", "1080p", "bluray", "x264"], ignoreOrder: true);
    }

    [Fact]
    public void DistinctTokens_BlankInput_ReturnsEmpty()
    {
        // act
        var tokens = SubtitleMatchScorer.DistinctTokens("   ");

        // assert
        tokens.ShouldBeEmpty();
    }

    [Fact]
    public void Compare_DifferentReleaseNames_OrdersOrdinal()
    {
        // act
        var result = SubtitleMatchScorer.Compare("Alpha", "b", "Beta", "a");

        // assert
        result.ShouldBeLessThan(0);
    }

    [Fact]
    public void Compare_SameReleaseName_OrdersByCandidateId()
    {
        // act
        var result = SubtitleMatchScorer.Compare("Same", "a1", "Same", "a2");

        // assert
        result.ShouldBeLessThan(0);
    }

    [Fact]
    public void Compare_NullReleaseNames_TreatsBothAsEmpty()
    {
        // act
        var result = SubtitleMatchScorer.Compare(null, "a", null, "b");

        // assert
        result.ShouldBeLessThan(0);
    }

    [Fact]
    public void Score_IsSymmetricForDistinctTokenOverlap()
    {
        // arrange
        const string left = "alpha beta gamma";
        const string right = "beta gamma delta epsilon";

        // act
        var forward = SubtitleMatchScorer.Score(left, right, matchesMovieHash: false);
        var reversed = SubtitleMatchScorer.Score(right, left, matchesMovieHash: false);

        // assert
        forward.ShouldBe(reversed);
    }

    [Fact]
    public void DistinctTokens_KeepsUnicodeLettersAndDigits()
    {
        // act
        var tokens = SubtitleMatchScorer.DistinctTokens("Português Ação 2021");

        // assert
        tokens.ShouldBe(["português", "ação", "2021"], ignoreOrder: true);
        tokens.Count.ShouldBe(3);
    }
}
