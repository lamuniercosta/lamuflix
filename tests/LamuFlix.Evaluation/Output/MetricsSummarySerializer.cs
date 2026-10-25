using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using LamuFlix.Evaluation.Metrics;

namespace LamuFlix.Evaluation.Output;

/// <summary>
/// TB002 serializer contract: valid compact System.Text.Json, indentation
/// disabled, UTF-8 without BOM or trailing newline, exact FR-006 field order,
/// finite invariant-culture JSON numbers with no manual rounding and no volatile
/// metadata. Writing goes through a sibling partial file so an ordinary write
/// failure can never leave a truncated summary at the target path.
/// </summary>
public static class MetricsSummarySerializer
{
    private const string PartialSuffix = ".partial";

    public static byte[] SerializeToUtf8Bytes(MetricsSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        Validate(summary);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", summary.SchemaVersion);
            writer.WriteString("fixtureId", summary.FixtureId);
            writer.WriteNumber("seed", summary.Seed);
            writer.WriteNumber("cutoff", summary.Cutoff);
            writer.WriteNumber("observationCount", summary.ObservationCount);
            writer.WriteNumber("hitCount", summary.HitCount);
            writer.WriteNumber("reciprocalRankSum", summary.ReciprocalRankSum);
            writer.WriteNumber("hitRateAt10", summary.HitRateAt10);
            writer.WriteNumber("mrr", summary.Mrr);
            writer.WriteEndObject();
        }

        var bytes = new byte[buffer.WrittenCount];
        buffer.WrittenSpan.CopyTo(bytes);

        return bytes;
    }

    public static void WriteToFile(MetricsSummary summary, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var bytes = SerializeToUtf8Bytes(summary);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new ArgumentException("The summary path must name a file inside a directory.", nameof(path));
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

    private static void Validate(MetricsSummary summary)
    {
        if (summary.SchemaVersion != MetricsSummary.CurrentSchemaVersion)
        {
            throw new ArgumentException(
                $"The metrics summary schemaVersion must be {MetricsSummary.CurrentSchemaVersion}.",
                nameof(summary));
        }

        if (string.IsNullOrWhiteSpace(summary.FixtureId))
        {
            throw new ArgumentException("The metrics summary fixtureId must not be blank.", nameof(summary));
        }

        if (summary.Cutoff != MetricCalculator.Cutoff)
        {
            throw new ArgumentException(
                $"The metrics summary cutoff must be {MetricCalculator.Cutoff}.",
                nameof(summary));
        }

        if (summary.ObservationCount < 0 || summary.HitCount < 0 || summary.HitCount > summary.ObservationCount)
        {
            throw new ArgumentException(
                "The metrics summary counts must be non-negative and hits cannot exceed observations.",
                nameof(summary));
        }

        EnsureFinite(summary.ReciprocalRankSum, nameof(summary.ReciprocalRankSum));
        EnsureFinite(summary.HitRateAt10, nameof(summary.HitRateAt10));
        EnsureFinite(summary.Mrr, nameof(summary.Mrr));
    }

    private static void EnsureFinite(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new ArgumentException($"The metrics summary {name} must be a finite non-negative number.", name);
        }
    }
}
