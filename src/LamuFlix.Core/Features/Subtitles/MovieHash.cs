using System;
using System.Buffers.Binary;
using System.Globalization;

namespace LamuFlix.Core.Features.Subtitles;

public static class MovieHash
{
    public const long MinimumEligibleLength = 131_072;
    public const int WindowLength = 65_536;

    private const int WordLength = 8;
    private const string HashFormat = "x16";

    public static bool IsEligible(long fileLength) => fileLength >= MinimumEligibleLength;

    public static string Compute(long fileLength, ReadOnlySpan<byte> head, ReadOnlySpan<byte> tail)
    {
        unchecked
        {
            var hash = (ulong)fileLength;
            hash = AddWords(hash, head);
            hash = AddWords(hash, tail);
            return hash.ToString(HashFormat, CultureInfo.InvariantCulture);
        }
    }

    private static ulong AddWords(ulong hash, ReadOnlySpan<byte> window)
    {
        unchecked
        {
            var words = window.Length / WordLength;
            for (var index = 0; index < words; index++)
            {
                hash += BinaryPrimitives.ReadUInt64LittleEndian(window[(index * WordLength)..]);
            }

            return hash;
        }
    }
}
