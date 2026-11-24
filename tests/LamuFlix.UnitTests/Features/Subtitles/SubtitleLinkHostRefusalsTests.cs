using System;
using LamuFlix.Core.Features.Subtitles;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleLinkHostRefusalsTests
{
    [Fact]
    public void TryGetRefused_FreshInstance_ReturnsFalse()
    {
        // arrange
        var refusals = new SubtitleLinkHostRefusals();

        // act
        var recorded = refusals.TryGetRefused(out var host);

        // assert
        recorded.ShouldBeFalse();
        host.ShouldBeNull();
    }

    [Fact]
    public void TryGetRefused_AfterRecord_ReturnsTheRecordedHost()
    {
        // arrange
        var refusals = new SubtitleLinkHostRefusals();
        refusals.Record("a.example");

        // act
        var recorded = refusals.TryGetRefused(out var host);

        // assert
        recorded.ShouldBeTrue();
        host.ShouldBe("a.example");
    }

    [Fact]
    public void TryGetRefused_LatestRecordWins()
    {
        // arrange
        var refusals = new SubtitleLinkHostRefusals();
        refusals.Record("a.example");
        refusals.Record("b.example");

        // act
        refusals.TryGetRefused(out var host);

        // assert
        host.ShouldBe("b.example");
    }

    [Fact]
    public void Record_NullHost_ThrowsArgumentNullException()
    {
        // arrange
        var refusals = new SubtitleLinkHostRefusals();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => refusals.Record(null!);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }
}
