using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;

namespace LamuFlix.Evaluation.RealLibrary;

public static class QualityReportSerializer
{
    private const string PartialSuffix = ".partial";

    public static byte[] SerializeToUtf8Bytes(QualityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("mode", "quality");
            WriteBars(writer, report);
            writer.WriteNumber("enrichedSelectedCount", report.EnrichedSelectedCount);
            WriteCoverage(writer, report.Coverage);
            WriteBands(writer, report.Bands);
            WriteJudgedPairs(writer, report.JudgedPairs);
            WriteDisclosures(writer, report.Disclosures);
            writer.WriteEndObject();
        }

        var bytes = new byte[buffer.WrittenCount];
        buffer.WrittenSpan.CopyTo(bytes);

        return bytes;
    }

    public static void WriteToFile(QualityReport report, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var bytes = SerializeToUtf8Bytes(report);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new ArgumentException("The report path must name a file inside a directory.", nameof(path));
        }

        Directory.CreateDirectory(directory);

        var partialPath = fullPath + PartialSuffix;
        try
        {
            File.WriteAllBytes(partialPath, bytes);
            File.Move(partialPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(partialPath))
            {
                File.Delete(partialPath);
            }
        }
    }

    private static void WriteBars(Utf8JsonWriter writer, QualityReport report)
    {
        writer.WriteStartArray("bars");

        foreach (var bar in report.Bars)
        {
            writer.WriteNumberValue(bar);
        }

        writer.WriteEndArray();
        writer.WriteNumber("strongOnlyBar", report.StrongOnlyBar);
    }

    private static void WriteCoverage(Utf8JsonWriter writer, ImmutableArray<CoverageLine> coverage)
    {
        writer.WriteStartArray("coverage");

        foreach (var line in coverage)
        {
            writer.WriteStartObject();
            writer.WriteNumber("bar", line.Bar);
            writer.WriteString("rule", line.Rule);
            writer.WriteStartObject("buckets");
            writer.WriteNumber("zero", line.Buckets.Zero);
            writer.WriteNumber("oneToThree", line.Buckets.OneToThree);
            writer.WriteNumber("fourToTen", line.Buckets.FourToTen);
            writer.WriteNumber("aboveTen", line.Buckets.AboveTen);
            writer.WriteEndObject();
            WriteNullableNumber(writer, "medianFirstScore", line.MedianFirstScore);
            writer.WriteNumber("firstScoreCount", line.FirstScoreCount);
            WriteNullableNumber(writer, "medianTenthScore", line.MedianTenthScore);
            writer.WriteNumber("tenthScoreCount", line.TenthScoreCount);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteBands(Utf8JsonWriter writer, ImmutableArray<BandSample> bands)
    {
        writer.WriteStartArray("bands");

        foreach (var band in bands)
        {
            writer.WriteStartObject();
            writer.WriteString("label", band.Label);
            WriteNullableNumber(writer, "lowerInclusive", band.LowerInclusive);
            WriteNullableNumber(writer, "upperExclusive", band.UpperExclusive);
            writer.WriteNumber("availableCount", band.AvailableCount);
            writer.WriteBoolean("undersupplied", band.Undersupplied);
            writer.WriteStartArray("pairs");

            foreach (var pair in band.Pairs)
            {
                writer.WriteStartObject();
                writer.WriteNumber("selectedId", pair.SelectedId);
                writer.WriteNumber("otherId", pair.OtherId);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteJudgedPairs(Utf8JsonWriter writer, JudgedPairReport judgedPairs)
    {
        writer.WriteStartObject("judgedPairs");
        WriteShares(writer, "good", judgedPairs.Good);
        WriteShares(writer, "bad", judgedPairs.Bad);
        writer.WriteStartArray("valid");

        foreach (var outcome in judgedPairs.Valid)
        {
            writer.WriteStartObject();
            writer.WriteNumber("movieId", outcome.Pair.MovieId);
            writer.WriteNumber("otherId", outcome.Pair.OtherId);
            writer.WriteString("verdict", outcome.Pair.Verdict == JudgedVerdict.Good ? "good" : "bad");
            WriteOptionalNote(writer, outcome.Pair.Note);
            writer.WriteNumber("rank", outcome.Rank);
            writer.WriteNumber("score", outcome.Score);
            writer.WriteBoolean("rankOne", outcome.IsRankOne);
            writer.WriteBoolean("topThree", outcome.IsTopThree);
            writer.WriteBoolean("strong", outcome.IsStrong);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("skipped");

        foreach (var skipped in judgedPairs.Skipped)
        {
            writer.WriteStartObject();
            writer.WriteNumber("movieId", skipped.Pair.MovieId);
            writer.WriteNumber("otherId", skipped.Pair.OtherId);
            writer.WriteString("verdict", skipped.Pair.Verdict == JudgedVerdict.Good ? "good" : "bad");
            writer.WriteString("reason", skipped.Reason);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteShares(Utf8JsonWriter writer, string verdict, JudgedShares shares)
    {
        writer.WriteStartObject(verdict);
        writer.WriteNumber("validCount", shares.ValidCount);
        WriteNullableNumber(writer, "rankOneShare", shares.RankOneShare);
        WriteNullableNumber(writer, "topThreeShare", shares.TopThreeShare);
        WriteNullableNumber(writer, "strongShare", shares.StrongShare);
        writer.WriteStartArray("barShares");

        foreach (var barShare in shares.BarShares)
        {
            writer.WriteStartObject();
            writer.WriteNumber("bar", barShare.Bar);
            WriteNullableNumber(writer, "share", barShare.Share);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteOptionalNote(Utf8JsonWriter writer, string? note)
    {
        if (note is null)
        {
            return;
        }

        writer.WriteString("note", note);
    }

    private static void WriteDisclosures(Utf8JsonWriter writer, ImmutableArray<string> disclosures)
    {
        writer.WriteStartArray("disclosures");

        foreach (var disclosure in disclosures)
        {
            writer.WriteStringValue(disclosure);
        }

        writer.WriteEndArray();
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, string name, double? value)
    {
        if (value is { } number && double.IsFinite(number))
        {
            writer.WriteNumber(name, number);
        }
        else
        {
            writer.WriteNull(name);
        }
    }
}
