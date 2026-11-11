using System;
using System.Text;
using System.Text.RegularExpressions;

namespace LamuFlix.Infrastructure.Adapters;

internal static partial class SubtitlePayloadValidator
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static readonly byte[][] BlockedPrefixes =
    [
        [(byte)'<'],
        [(byte)'{'],
        [(byte)'['],
        [(byte)'P', (byte)'K'],
        [0x1F, 0x8B],
    ];

    public static bool IsAcceptable(ReadOnlySpan<byte> payload, long maxBytes)
    {
        if (payload.IsEmpty || payload.Length > maxBytes)
        {
            return false;
        }

        if (HasBlockedPrefix(payload))
        {
            return false;
        }

        var body = StripByteOrderMark(payload);
        return TryDecode(body, out var text) && CueLine().IsMatch(text);
    }

    private static bool HasBlockedPrefix(ReadOnlySpan<byte> payload)
    {
        foreach (var prefix in BlockedPrefixes)
        {
            if (payload.Length >= prefix.Length && payload[..prefix.Length].SequenceEqual(prefix))
            {
                return true;
            }
        }

        return false;
    }

    private static ReadOnlySpan<byte> StripByteOrderMark(ReadOnlySpan<byte> payload) =>
        payload.Length >= 3 && payload[0] == 0xEF && payload[1] == 0xBB && payload[2] == 0xBF
            ? payload[3..]
            : payload;

    private static bool TryDecode(ReadOnlySpan<byte> payload, out string text)
    {
        try
        {
            text = StrictUtf8.GetString(payload);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
    }

    [GeneratedRegex(@"[0-9]{2}:[0-9]{2}:[0-9]{2}[,.][0-9]{3}[^\S\r\n]*-->[^\S\r\n]*[0-9]{2}:[0-9]{2}:[0-9]{2}[,.][0-9]{3}", RegexOptions.CultureInvariant)]
    private static partial Regex CueLine();
}
