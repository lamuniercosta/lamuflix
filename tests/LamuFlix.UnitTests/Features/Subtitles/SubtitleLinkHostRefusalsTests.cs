using System;
using System.Linq;
using System.Threading.Tasks;
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
    public void TryRecord_FirstWrite_ReturnsTrueAndIsReadable()
    {
        // arrange
        var refusals = new SubtitleLinkHostRefusals();

        // act
        var won = refusals.TryRecord("a.example");

        // assert
        won.ShouldBeTrue();
        refusals.TryGetRefused(out var host).ShouldBeTrue();
        host.ShouldBe("a.example");
    }

    [Fact]
    public void TryRecord_SecondDifferentHost_ReturnsFalseAndKeepsTheFirst()
    {
        // arrange
        var refusals = new SubtitleLinkHostRefusals();
        refusals.TryRecord("a.example");

        // act
        var won = refusals.TryRecord("b.example");

        // assert
        won.ShouldBeFalse();
        refusals.TryGetRefused(out var host);
        host.ShouldBe("a.example");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void TryRecord_EmptyOrWhitespaceHost_ThrowsArgumentException(string host)
    {
        // arrange
        var refusals = new SubtitleLinkHostRefusals();

        // act
        Action act = () => refusals.TryRecord(host);

        // assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void TryRecord_NullHost_ThrowsArgumentNullException()
    {
        // arrange
        var refusals = new SubtitleLinkHostRefusals();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        Action act = () => refusals.TryRecord(null!);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void TryRecord_ConcurrentWriters_ExactlyOneWinsAndTheWinnerIsStored()
    {
        // arrange
        var refusals = new SubtitleLinkHostRefusals();
        var results = new bool[64];

        // act
        Parallel.For(0, results.Length, i => results[i] = refusals.TryRecord($"host{i}.example"));

        // assert
        results.Count(won => won).ShouldBe(1);
        refusals.TryGetRefused(out var host).ShouldBeTrue();
        host.ShouldBe($"host{Array.IndexOf(results, true)}.example");
    }
}
