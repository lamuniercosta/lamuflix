using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using Shouldly;
using Xunit;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleTrackTokenTests
{
    private const int MovieIdValue = 42;
    private const string FileName = "Heat.1995.1080p.en.srt";

    [Fact]
    public void For_SameMovieAndFileName_IsDeterministicAndUnpaddedBase64Url()
    {
        // act
        var first = SubtitleTrackToken.For(new MovieId(MovieIdValue), FileName);
        var second = SubtitleTrackToken.For(new MovieId(MovieIdValue), FileName);

        // assert
        second.ShouldBe(first);
        first.ShouldNotContain("=");
        first.ShouldNotContain("+");
        first.ShouldNotContain("/");
        first.Length.ShouldBe(43);
    }

    [Fact]
    public void For_DifferentFileNames_ProduceDistinctTokens()
    {
        // act
        var english = SubtitleTrackToken.For(new MovieId(MovieIdValue), FileName);
        var portuguese = SubtitleTrackToken.For(new MovieId(MovieIdValue), "Heat.1995.1080p.pob.srt");

        // assert
        portuguese.ShouldNotBe(english);
    }

    [Fact]
    public void For_SameFileNameDifferentMovies_ProduceDistinctTokens()
    {
        // act
        var first = SubtitleTrackToken.For(new MovieId(MovieIdValue), FileName);
        var second = SubtitleTrackToken.For(new MovieId(MovieIdValue + 1), FileName);

        // assert
        second.ShouldNotBe(first);
    }

    [Fact]
    public void For_IsNeverAnOrdinalIndex()
    {
        // act
        var token = SubtitleTrackToken.For(new MovieId(MovieIdValue), FileName);

        // assert
        token.ShouldNotBe("0");
        token.ShouldNotBe("1");
    }

    [Fact]
    public void For_MovieIdAndFileNameFraming_IsUnambiguousAcrossBoundaries()
    {
        // act — "4" + "2..." must not collide with "42" + "..."
        var left = SubtitleTrackToken.For(new MovieId(4), "2" + FileName);
        var right = SubtitleTrackToken.For(new MovieId(42), FileName);

        // assert
        left.ShouldNotBe(right);
    }
}
