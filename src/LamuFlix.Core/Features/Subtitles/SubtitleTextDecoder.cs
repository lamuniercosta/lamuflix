using System;
using System.Text;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Subtitles;

/// <summary>
/// Decodes sidecar subtitle bytes with the frozen policy: unsupported UTF-16/UTF-32 byte order marks are
/// rejected, otherwise strict UTF-8 is attempted (an optional UTF-8 BOM is stripped) and Windows-1252 is
/// used only when strict UTF-8 fails. No statistical guessing and no replacement-character success.
/// Newlines are normalized to LF.
/// </summary>
public static class SubtitleTextDecoder
{
    private const int Windows1252CodePage = 1252;
    private const byte Utf8BomFirst = 0xEF;
    private const byte Utf8BomSecond = 0xBB;
    private const byte Utf8BomThird = 0xBF;

    private static readonly UTF8Encoding StrictUtf8 =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static string Decode(ReadOnlySpan<byte> payload)
    {
        if (HasUnsupportedByteOrderMark(payload))
        {
            throw UnsupportedEncoding();
        }

        var body = StripUtf8ByteOrderMark(payload);
        return TryDecodeStrictUtf8(body, out var text)
            ? NormalizeNewlines(text)
            : NormalizeNewlines(DecodeWindows1252(body));
    }

    private static bool HasUnsupportedByteOrderMark(ReadOnlySpan<byte> payload) =>
        HasUtf16ByteOrderMark(payload) || HasUtf32ByteOrderMark(payload);

    private static bool HasUtf16ByteOrderMark(ReadOnlySpan<byte> payload) =>
        payload.Length >= 2
        && ((payload[0] == 0xFF && payload[1] == 0xFE)
            || (payload[0] == 0xFE && payload[1] == 0xFF));

    private static bool HasUtf32ByteOrderMark(ReadOnlySpan<byte> payload) =>
        payload.Length >= 4
        && ((payload[0] == 0x00 && payload[1] == 0x00 && payload[2] == 0xFE && payload[3] == 0xFF)
            || (payload[0] == 0xFF && payload[1] == 0xFE && payload[2] == 0x00 && payload[3] == 0x00));

    private static ReadOnlySpan<byte> StripUtf8ByteOrderMark(ReadOnlySpan<byte> payload) =>
        payload.Length >= 3 && payload[0] == Utf8BomFirst && payload[1] == Utf8BomSecond && payload[2] == Utf8BomThird
            ? payload[3..]
            : payload;

    private static bool TryDecodeStrictUtf8(ReadOnlySpan<byte> payload, out string text)
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

    private static string DecodeWindows1252(ReadOnlySpan<byte> payload)
    {
        var encoding = CodePagesEncodingProvider.Instance.GetEncoding(Windows1252CodePage)
                       ?? throw UnsupportedEncoding();
        return encoding.GetString(payload);
    }

    private static string NormalizeNewlines(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static ValidationException UnsupportedEncoding() =>
        new(SubtitlePayloadErrors.Create());
}
