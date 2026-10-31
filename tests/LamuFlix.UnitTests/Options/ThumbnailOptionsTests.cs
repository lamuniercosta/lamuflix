using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class ThumbnailOptionsTests
{
    [Fact]
    public void SectionName_IsThumbnails()
    {
        // act
        var sectionName = ThumbnailOptions.SectionName;

        // assert
        sectionName.ShouldBe("Thumbnails");
    }

    [Fact]
    public void Defaults_MatchApprovedValues()
    {
        // act
        var options = new ThumbnailOptions();

        // assert
        options.Enabled.ShouldBeFalse();
        options.FfmpegPath.ShouldBe("ffmpeg");
        options.CacheDirectory.ShouldBe("cache/thumbnails");
        options.MaxWidth.ShouldBe(320);
        options.ProcessTimeoutSeconds.ShouldBe(30);
    }
}
