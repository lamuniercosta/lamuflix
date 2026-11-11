using System;
using System.Text;
using LamuFlix.Core.Features.Subtitles;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class MovieHashTests
{
    private const int Window = MovieHash.WindowLength;

    public static TheoryData<long, string, string, string> ReferenceVectors => new()
    {
        { 131_072, "zeros", "0000000000020000", "all-zero 131072" },
        { 131_072, "ones", "4040404040424000", "all-one 131072" },
        { 131_072, "increment", "a0601fdf9f610000", "byte-increment 131072" },
        { 200_000, "zeros", "0000000000030d40", "all-zero 200000" },
    };

    [Theory]
    [MemberData(nameof(ReferenceVectors))]
    public void Compute_ReferenceVector_ReturnsTheExpectedSixteenHexDigits(
        long fileLength,
        string pattern,
        string expected,
        string because)
    {
        // arrange
        var payload = Payload(pattern, fileLength);
        var head = payload.AsSpan(0, Window);
        var tail = payload.AsSpan(payload.Length - Window, Window);

        // act
        var hash = MovieHash.Compute(fileLength, head, tail);

        // assert
        hash.ShouldBe(expected, because);
        hash.Length.ShouldBe(16);
        hash.ShouldBe(hash.ToLowerInvariant());
    }

    [Fact]
    public void Compute_OnlyTheFirstHeadByteSet_ShiftsOnlyTheLowestNibble()
    {
        // arrange
        var payload = Payload("zeros", 131_072);
        payload[0] = 1;

        // act
        var hash = MovieHash.Compute(
            payload.Length,
            payload.AsSpan(0, Window),
            payload.AsSpan(payload.Length - Window, Window));

        // assert
        hash.ShouldBe("0000000000020001");
    }

    [Fact]
    public void Compute_HighestBitOfTheLastTailByteSet_SetsTheTopBit()
    {
        // arrange
        var payload = Payload("zeros", 131_072);
        payload[^1] = 0x80;

        // act
        var hash = MovieHash.Compute(
            payload.Length,
            payload.AsSpan(0, Window),
            payload.AsSpan(payload.Length - Window, Window));

        // assert
        hash.ShouldBe("8000000000020000");
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(65_536L)]
    [InlineData(131_071L)]
    public void IsEligible_BelowTheMinimumLength_ReturnsFalse(long fileLength)
    {
        // act
        var eligible = MovieHash.IsEligible(fileLength);

        // assert
        eligible.ShouldBeFalse();
    }

    [Theory]
    [InlineData(131_072L)]
    [InlineData(131_073L)]
    [InlineData(4_294_967_296L)]
    public void IsEligible_AtOrAboveTheMinimumLength_ReturnsTrue(long fileLength)
    {
        // act
        var eligible = MovieHash.IsEligible(fileLength);

        // assert
        eligible.ShouldBeTrue();
    }

    [Fact]
    public void Compute_OverlappingHeadAndTailWindows_CountsBothWindowSums()
    {
        // arrange
        var payload = Payload("ones", 131_072);

        // act
        var hash = MovieHash.Compute(
            payload.Length,
            payload.AsSpan(0, Window),
            payload.AsSpan(0, Window));

        // assert
        hash.ShouldBe("4040404040424000");
    }

    [Fact]
    public void Compute_LengthBeyondTheWindowSum_KeepsTheLengthHighWord()
    {
        // arrange
        var head = new byte[Window];
        var tail = new byte[Window];

        // act
        var hash = MovieHash.Compute(4_294_967_296L, head, tail);

        // assert
        hash.ShouldBe("0000000100000000");
    }

    internal static byte[] Payload(string pattern, long length) =>
        pattern switch
        {
            "ones" => Filled(length, 0x01),
            "increment" => Incrementing(length),
            _ => new byte[length],
        };

    private static byte[] Filled(long length, byte value)
    {
        var payload = new byte[length];
        Array.Fill(payload, value);
        return payload;
    }

    private static byte[] Incrementing(long length)
    {
        var payload = new byte[length];
        for (var index = 0; index < payload.Length; index++)
        {
            payload[index] = (byte)(index % 256);
        }

        return payload;
    }

    [Fact]
    public void Compute_NegativeLength_StillReturnsSixteenHexDigits()
    {
        // arrange
        var head = Array.Empty<byte>();

        // act
        var hash = MovieHash.Compute(-1, head, head);

        // assert
        hash.ShouldBe("ffffffffffffffff");
        Encoding.UTF8.GetByteCount(hash).ShouldBe(16);
    }
}
