using System;
using System.Buffers.Binary;
using FsCheck.Xunit;
using LamuFlix.Core.Features.Subtitles;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class MovieHashPropertyTests
{
    private const int Window = MovieHash.WindowLength;
    private const ulong Mask = ulong.MaxValue;

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Compute_AlwaysReturnsSixteenLowercaseHexDigits(long length, int headSeed, int tailSeed)
    {
        // arrange
        var head = RandomWindow(headSeed);
        var tail = RandomWindow(tailSeed);

        // act
        var hash = MovieHash.Compute(length, head, tail);

        // assert
        return hash.Length == 16
            && hash.Equals(hash.ToLowerInvariant(), StringComparison.Ordinal)
            && ulong.TryParse(hash, System.Globalization.NumberStyles.HexNumber, null, out _);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Compute_MatchesTheReferenceWordSum(long length, int headSeed, int tailSeed)
    {
        // arrange
        var head = RandomWindow(headSeed);
        var tail = RandomWindow(tailSeed);
        var expected = unchecked((ulong)length + WordSum(head) + WordSum(tail));

        // act
        var hash = MovieHash.Compute(length, head, tail);

        // assert
        return string.Equals(hash, expected.ToString("x16", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Compute_SwappingTheWindowsCommutes(long length, int headSeed, int tailSeed)
    {
        // arrange
        var head = RandomWindow(headSeed);
        var tail = RandomWindow(tailSeed);

        // act
        var forward = MovieHash.Compute(length, head, tail);
        var reversed = MovieHash.Compute(length, tail, head);

        // assert
        return string.Equals(forward, reversed, StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Compute_ChangesWheneverOneHeadByteChanges(long length, int seed, byte replacement)
    {
        // arrange
        var head = RandomWindow(seed);
        var tail = RandomWindow(seed + 1);
        var mutated = (byte[])head.Clone();
        mutated[0] = replacement;
        var original = MovieHash.Compute(length, head, tail);

        // act
        var changed = MovieHash.Compute(length, mutated, tail);

        // assert
        return replacement == head[0] || !string.Equals(original, changed, StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool IsEligible_MatchesTheMinimumLengthBoundary(long length)
    {
        // act
        var eligible = MovieHash.IsEligible(length);

        // assert
        return eligible == (length >= MovieHash.MinimumEligibleLength);
    }

    private static byte[] RandomWindow(int seed)
    {
        var window = new byte[Window];
        var state = (ulong)(uint)seed | 1UL;
        for (var offset = 0; offset < window.Length; offset += sizeof(ulong))
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            BinaryPrimitives.WriteUInt64LittleEndian(window.AsSpan(offset), state & Mask);
        }

        return window;
    }

    private static ulong WordSum(byte[] window)
    {
        var sum = 0UL;
        for (var offset = 0; offset < window.Length; offset += sizeof(ulong))
        {
            sum = unchecked(sum + BinaryPrimitives.ReadUInt64LittleEndian(window.AsSpan(offset)));
        }

        return sum;
    }
}
