using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Subtitles;

/// <summary>Configured conversion bounds enforced while parsing and emitting WebVTT.</summary>
public sealed record SubtitleConversionLimits(int MaxCues, int MaxLineCharacters, int MaxOutputBytes);

/// <summary>
/// Converts an SRT document into literal WebVTT: <c>WEBVTT</c> plus a blank line, cue order and timing
/// preserved, sequence indices dropped, comma milliseconds converted to dot milliseconds, and
/// <c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c> escaped so cue text renders literally. Valid overlapping cues stay
/// valid and NOTE/STYLE/REGION-looking words inside a cue remain literal text. Only structures that cannot
/// be represented as literal WebVTT are rejected, always with a sanitized payload error.
/// </summary>
public static class SubtitleWebVttConverter
{
    private const string Header = "WEBVTT\n\n";
    private const string TimingArrow = " --> ";
    private const string SourceArrow = "-->";
    private const char CommaSeparator = ',';
    private const char TimeSeparator = ':';
    private const int MillisecondsPerSecond = 1000;
    private const int SecondsPerMinute = 60;
    private const int MinutesPerHour = 60;
    private const int FractionLength = 3;
    private const int MaxHours = 9999;
    private const long InvalidTimestamp = -1;

    public static string Convert(string document, SubtitleConversionLimits limits)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(limits);

        var cues = ParseCues(document, limits);
        return Render(cues, limits.MaxOutputBytes);
    }

    private static List<SubtitleCue> ParseCues(string document, SubtitleConversionLimits limits)
    {
        var lines = document
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        var cues = new List<SubtitleCue>();
        var block = new List<string>();
        for (var index = 0; index <= lines.Length; index++)
        {
            if (index < lines.Length && !IsBlank(lines[index]))
            {
                block.Add(lines[index]);
                continue;
            }

            AddBlock(block, cues, limits);
        }

        if (cues.Count == 0)
        {
            throw SubtitlePayloadErrors.Invalid();
        }

        return cues;
    }

    private static void AddBlock(List<string> block, List<SubtitleCue> cues, SubtitleConversionLimits limits)
    {
        if (block.Count == 0)
        {
            return;
        }

        if (block.Count < 3 || !IsSequenceIndex(block[0]))
        {
            throw SubtitlePayloadErrors.Invalid();
        }

        var start = ParseTimestamp(block[1], out var end);
        if (start == InvalidTimestamp || end < start)
        {
            throw SubtitlePayloadErrors.Invalid();
        }

        if (cues.Count == limits.MaxCues)
        {
            throw SubtitlePayloadErrors.Invalid();
        }

        var payload = block.GetRange(2, block.Count - 2);
        ValidatePayload(payload, limits);

        cues.Add(new SubtitleCue(FormatTimestamp(start), FormatTimestamp(end), payload));
        block.Clear();
    }

    private static void ValidatePayload(List<string> payload, SubtitleConversionLimits limits)
    {
        foreach (var line in payload)
        {
            if (line.Contains(SourceArrow, StringComparison.Ordinal) || line.Length > limits.MaxLineCharacters)
            {
                throw SubtitlePayloadErrors.Invalid();
            }
        }
    }

    private static bool IsBlank(string line) => line.Trim().Length == 0;

    private static bool IsSequenceIndex(string line)
    {
        if (line.Length == 0 || line.Length > 9)
        {
            return false;
        }

        foreach (var character in line)
        {
            if (!char.IsAsciiDigit(character))
            {
                return false;
            }
        }

        return true;
    }

    private static long ParseTimestamp(string line, out long end)
    {
        end = InvalidTimestamp;
        var arrow = line.IndexOf(SourceArrow, StringComparison.Ordinal);
        if (arrow < 0)
        {
            return InvalidTimestamp;
        }

        var start = ParseValue(line[..arrow].Trim());
        end = ParseValue(line[(arrow + SourceArrow.Length)..].Trim());
        return start;
    }

    private static long ParseValue(string value)
    {
        var parts = value.Split(TimeSeparator);
        if (parts.Length != 3
            || !TryParseNumber(parts[0], MaxHours, out var hours)
            || !TryParseNumber(parts[1], SecondsPerMinute - 1, out var minutes)
            || !TryParseSeconds(parts[2], out var seconds, out var milliseconds))
        {
            return InvalidTimestamp;
        }

        return (((long)hours * MinutesPerHour) + minutes) * SecondsPerMinute * MillisecondsPerSecond
               + (seconds * MillisecondsPerSecond)
               + milliseconds;
    }

    private static bool TryParseSeconds(string value, out int seconds, out int milliseconds)
    {
        seconds = 0;
        milliseconds = 0;
        var separator = value.IndexOf(CommaSeparator);
        if (separator < 0)
        {
            return false;
        }

        var fraction = value[(separator + 1)..];
        return fraction.Length == FractionLength
               && TryParseNumber(fraction, 999, out milliseconds)
               && TryParseNumber(value[..separator], SecondsPerMinute - 1, out seconds);
    }

    private static bool TryParseNumber(string value, int maximum, out int result)
    {
        result = 0;
        return value.Length is > 0 and <= 4
               && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result)
               && result <= maximum;
    }

    private static string FormatTimestamp(long totalMilliseconds)
    {
        var seconds = totalMilliseconds / MillisecondsPerSecond;
        var milliseconds = totalMilliseconds % MillisecondsPerSecond;
        var minutes = seconds / SecondsPerMinute;
        var hours = minutes / MinutesPerHour;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{hours:00}:{minutes % MinutesPerHour:00}:{seconds % SecondsPerMinute:00}.{milliseconds:000}");
    }

    private static string Render(List<SubtitleCue> cues, int maxOutputBytes)
    {
        var builder = new StringBuilder();
        var used = 0;
        Append(builder, Header, maxOutputBytes, ref used);
        foreach (var cue in cues)
        {
            Append(builder, cue.Start, maxOutputBytes, ref used);
            Append(builder, TimingArrow, maxOutputBytes, ref used);
            Append(builder, cue.End, maxOutputBytes, ref used);
            foreach (var line in cue.Payload)
            {
                Append(builder, "\n", maxOutputBytes, ref used);
                AppendEscaped(builder, line, maxOutputBytes, ref used);
            }

            Append(builder, "\n\n", maxOutputBytes, ref used);
        }

        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string text, int maxOutputBytes, ref int used)
    {
        var size = Encoding.UTF8.GetByteCount(text);
        if (used + size > maxOutputBytes)
        {
            throw SubtitlePayloadErrors.Invalid();
        }

        used += size;
        builder.Append(text);
    }

    private static void AppendEscaped(StringBuilder builder, string line, int maxOutputBytes, ref int used)
    {
        foreach (var character in line)
        {
            Append(builder, Escape(character), maxOutputBytes, ref used);
        }
    }

    private static string Escape(char character) => character switch
    {
        '&' => "&amp;",
        '<' => "&lt;",
        '>' => "&gt;",
        _ => character.ToString(),
    };

    private sealed record SubtitleCue(string Start, string End, IReadOnlyList<string> Payload);
}
