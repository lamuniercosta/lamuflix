using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;

namespace LamuFlix.Evaluation.RealLibrary;

public static class LeaveOneOutReportSerializer
{
    private const string PartialSuffix = ".partial";

    public static byte[] SerializeToUtf8Bytes(LeaveOneOutResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("mode", "leave-one-out");
            writer.WriteStartArray("cohorts");
            WriteCohort(writer, "liked", result.Liked);
            WriteCohort(writer, "watched", result.Watched);
            writer.WriteEndArray();
            WriteOverlap(writer, result.Overlap);
            WriteDisclosures(writer, result.Disclosures);
            writer.WriteEndObject();
        }

        var bytes = new byte[buffer.WrittenCount];
        buffer.WrittenSpan.CopyTo(bytes);

        return bytes;
    }

    public static void WriteToFile(LeaveOneOutResult result, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var bytes = SerializeToUtf8Bytes(result);
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

    private static void WriteCohort(Utf8JsonWriter writer, string name, CohortResult cohort)
    {
        writer.WriteStartObject();
        writer.WriteString("cohort", name);
        writer.WriteNumber("attemptedCount", cohort.Metrics.AttemptedCount);
        WriteNullableNumber(writer, "hitRateAt1", cohort.Metrics.HitRateAt1);
        WriteNullableNumber(writer, "hitRateAt3", cohort.Metrics.HitRateAt3);
        WriteNullableNumber(writer, "hitRateAt10", cohort.Metrics.HitRateAt10);
        WriteNullableNumber(writer, "mrr", cohort.Metrics.Mrr);
        writer.WriteStartArray("attempts");

        foreach (var attempt in cohort.Attempts)
        {
            WriteAttempt(writer, attempt);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteAttempt(Utf8JsonWriter writer, LeaveOneOutAttempt attempt)
    {
        writer.WriteStartObject();
        writer.WriteNumber("seedId", attempt.Seed.Value);
        writer.WriteNumber("take", attempt.Take);
        writer.WriteNumber("rank", attempt.Rank);
        writer.WriteStartArray("ranking");

        foreach (var movieId in attempt.Ranking)
        {
            writer.WriteNumberValue(movieId.Value);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteOverlap(Utf8JsonWriter writer, CohortOverlap overlap)
    {
        writer.WriteStartObject("overlap");
        writer.WriteNumber("likedCount", overlap.LikedCount);
        writer.WriteNumber("watchedCount", overlap.WatchedCount);
        writer.WriteNumber("overlapCount", overlap.OverlapCount);
        writer.WriteEndObject();
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
