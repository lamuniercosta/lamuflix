using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;

namespace LamuFlix.Evaluation.RealLibrary;

public static class RecommendationArtifactSerializer
{
    private const string PartialSuffix = ".partial";

    public static byte[] SerializeToUtf8Bytes(RecommendationArtifactEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        Validate(envelope);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            WriteEnvelope(writer, envelope);
        }

        var bytes = new byte[buffer.WrittenCount];
        buffer.WrittenSpan.CopyTo(bytes);

        return bytes;
    }

    public static void WriteToFile(RecommendationArtifactEnvelope envelope, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var bytes = SerializeToUtf8Bytes(envelope);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new ArgumentException("The artifact path must name a file inside a directory.", nameof(path));
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

    public static RecommendationArtifactEnvelope ReadFromUtf8Bytes(ReadOnlySpan<byte> json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json.ToArray());
        }
        catch (JsonException exception)
        {
            throw new RecommendationArtifactFormatException("The artifact is not valid JSON.", exception);
        }

        using (document)
        {
            return ReadEnvelope(document.RootElement);
        }
    }

    public static RecommendationArtifactEnvelope ReadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return ReadFromUtf8Bytes(File.ReadAllBytes(path));
    }

    private static void WriteEnvelope(Utf8JsonWriter writer, RecommendationArtifactEnvelope envelope)
    {
        writer.WriteStartObject();
        writer.WriteString("mode", envelope.Mode);
        writer.WriteStartArray("targets");
        foreach (var target in envelope.Targets)
        {
            writer.WriteNumberValue(target);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("lists");
        foreach (var list in envelope.Lists)
        {
            WriteList(writer, list);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteList(Utf8JsonWriter writer, RecommendationArtifactList list)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", list.Kind);
        WriteNullableNumber(writer, "targetId", list.TargetId);
        WriteNullableNumber(writer, "take", list.Take);
        writer.WriteString("status", list.Status);
        writer.WriteStartArray("rows");
        foreach (var row in list.Rows)
        {
            WriteRow(writer, row);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteRow(Utf8JsonWriter writer, RecommendationArtifactRow row)
    {
        writer.WriteStartObject();
        writer.WriteNumber("rank", row.Rank);
        writer.WriteNumber("movieId", row.MovieId);
        writer.WriteString("title", row.Title);
        writer.WriteNumber("score", row.Score);
        writer.WriteStartArray("reasons");
        foreach (var reason in row.Reasons)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", reason.Kind);
            writer.WriteString("text", reason.Text);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        if (row.Diagnostics is { } diagnostics)
        {
            WriteDiagnostics(writer, diagnostics);
        }

        writer.WriteEndObject();
    }

    private static void WriteDiagnostics(Utf8JsonWriter writer, RecommendationPairDiagnostics diagnostics)
    {
        writer.WriteStartObject("diagnostics");
        writer.WriteStartArray("breakdown");
        foreach (var criterion in diagnostics.Breakdown)
        {
            writer.WriteStartObject();
            writer.WriteString("criterion", criterion.Criterion);
            writer.WriteNumber("value", criterion.Value);
            writer.WriteNumber("weight", criterion.Weight);
            writer.WriteNumber("contribution", criterion.Contribution);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        if (diagnostics.RawPlotCosine is { } cosine)
        {
            writer.WriteNumber("rawPlotCosine", cosine);
        }
        else
        {
            writer.WriteNull("rawPlotCosine");
        }

        if (diagnostics.PlotRank is { } plotRank)
        {
            writer.WriteNumber("plotRank", plotRank);
        }
        else
        {
            writer.WriteNull("plotRank");
        }

        writer.WriteBoolean("inPool", diagnostics.InPool);
        writer.WriteEndObject();
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, string name, int? value)
    {
        if (value is { } number)
        {
            writer.WriteNumber(name, number);
        }
        else
        {
            writer.WriteNull(name);
        }
    }

    private static RecommendationArtifactEnvelope ReadEnvelope(JsonElement root)
    {
        EnsureObject(root);
        var envelope = new RecommendationArtifactEnvelope(
            ReadString(root, "mode"),
            ReadTargets(root),
            ReadLists(root));

        try
        {
            Validate(envelope);
        }
        catch (ArgumentException exception)
        {
            throw new RecommendationArtifactFormatException(
                $"The artifact violates the capture/compare contract: {exception.Message}",
                exception);
        }

        return envelope;
    }

    private static ImmutableArray<int> ReadTargets(JsonElement root)
    {
        var element = RequiredProperty(root, "targets");
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new RecommendationArtifactFormatException("The artifact targets must be an array.");
        }

        var builder = ImmutableArray.CreateBuilder<int>();
        foreach (var item in element.EnumerateArray())
        {
            builder.Add(ReadInt32(item, "targets"));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<RecommendationArtifactList> ReadLists(JsonElement root)
    {
        var element = RequiredProperty(root, "lists");
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new RecommendationArtifactFormatException("The artifact lists must be an array.");
        }

        var builder = ImmutableArray.CreateBuilder<RecommendationArtifactList>();
        foreach (var item in element.EnumerateArray())
        {
            EnsureObject(item);
            builder.Add(
                new RecommendationArtifactList(
                    ReadString(item, "kind"),
                    ReadNullableInt32(item, "targetId"),
                    ReadNullableInt32(item, "take"),
                    ReadString(item, "status"),
                    ReadRows(item)));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<RecommendationArtifactRow> ReadRows(JsonElement list)
    {
        var element = RequiredProperty(list, "rows");
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new RecommendationArtifactFormatException("The artifact rows must be an array.");
        }

        var builder = ImmutableArray.CreateBuilder<RecommendationArtifactRow>();
        foreach (var item in element.EnumerateArray())
        {
            EnsureObject(item);
            builder.Add(
                new RecommendationArtifactRow(
                    ReadInt32(RequiredProperty(item, "rank"), "rank"),
                    ReadInt32(RequiredProperty(item, "movieId"), "movieId"),
                    ReadString(item, "title"),
                    ReadDouble(RequiredProperty(item, "score"), "score"),
                    ReadReasons(item),
                    ReadDiagnostics(item)));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<RecommendationArtifactReason> ReadReasons(JsonElement row)
    {
        var element = RequiredProperty(row, "reasons");
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new RecommendationArtifactFormatException("The artifact reasons must be an array.");
        }

        var builder = ImmutableArray.CreateBuilder<RecommendationArtifactReason>();
        foreach (var item in element.EnumerateArray())
        {
            EnsureObject(item);
            builder.Add(new RecommendationArtifactReason(ReadString(item, "kind"), ReadString(item, "text")));
        }

        return builder.ToImmutable();
    }

    private static RecommendationPairDiagnostics? ReadDiagnostics(JsonElement row)
    {
        if (!row.TryGetProperty("diagnostics", out var element) || element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        EnsureObject(element);
        var breakdown = RequiredProperty(element, "breakdown");
        if (breakdown.ValueKind != JsonValueKind.Array)
        {
            throw new RecommendationArtifactFormatException("The pair diagnostics breakdown must be an array.");
        }

        var builder = ImmutableArray.CreateBuilder<RecommendationCriterionContribution>();
        foreach (var item in breakdown.EnumerateArray())
        {
            EnsureObject(item);
            builder.Add(
                new RecommendationCriterionContribution(
                    ReadString(item, "criterion"),
                    ReadDouble(RequiredProperty(item, "value"), "value"),
                    ReadDouble(RequiredProperty(item, "weight"), "weight"),
                    ReadDouble(RequiredProperty(item, "contribution"), "contribution")));
        }

        return new RecommendationPairDiagnostics(
            builder.ToImmutable(),
            ReadNullableDouble(element, "rawPlotCosine"),
            ReadNullableInt32(element, "plotRank"),
            ReadBoolean(element, "inPool"));
    }

    private static void Validate(RecommendationArtifactEnvelope envelope)
    {
        if (!IsKnownMode(envelope.Mode))
        {
            throw new ArgumentException(
                $"The artifact mode must be '{RecommendationArtifactContract.CaptureMode}' or "
                + $"'{RecommendationArtifactContract.CompareMode}'.",
                nameof(envelope));
        }

        if (envelope.Targets.IsDefaultOrEmpty)
        {
            throw new ArgumentException("The artifact must declare at least one target.", nameof(envelope));
        }

        EnsureUniqueTargets(envelope.Targets);

        if (envelope.Lists.IsDefaultOrEmpty)
        {
            throw new ArgumentException("The artifact must declare at least one list.", nameof(envelope));
        }

        foreach (var list in envelope.Lists)
        {
            ValidateList(envelope.Mode, list);
        }
    }

    private static void EnsureUniqueTargets(ImmutableArray<int> targets)
    {
        var seen = new HashSet<int>();
        foreach (var target in targets)
        {
            if (target <= 0)
            {
                throw new ArgumentException($"The artifact target '{target}' must be a positive integer.", nameof(targets));
            }

            if (!seen.Add(target))
            {
                throw new ArgumentException($"The artifact target '{target}' appears more than once.", nameof(targets));
            }
        }
    }

    private static void ValidateList(string mode, RecommendationArtifactList list)
    {
        if (!IsKnownListKind(list.Kind))
        {
            throw new ArgumentException($"The artifact list kind '{list.Kind}' is not recognized.", nameof(list));
        }

        if (list.Take is <= 0)
        {
            throw new ArgumentException("The artifact list take must be a positive integer when declared.", nameof(list));
        }

        if (list.Status is not (RecommendationArtifactContract.OkStatus or RecommendationArtifactContract.EmptyStatus))
        {
            throw new ArgumentException($"The artifact list status '{list.Status}' is not recognized.", nameof(list));
        }

        if (list.Status == RecommendationArtifactContract.EmptyStatus && !list.Rows.IsDefaultOrEmpty)
        {
            throw new ArgumentException("An empty artifact list must not carry rows.", nameof(list));
        }

        ValidateRows(mode, list);
    }

    private static void ValidateRows(string mode, RecommendationArtifactList list)
    {
        var seen = new HashSet<int>();
        for (var index = 0; index < list.Rows.Length; index++)
        {
            var row = list.Rows[index];
            if (row.Rank != index + 1)
            {
                throw new ArgumentException(
                    $"The artifact list '{list.Kind}' ranks must be one-based and contiguous; found {row.Rank} at position {index + 1}.",
                    nameof(list));
            }

            if (!seen.Add(row.MovieId))
            {
                throw new ArgumentException(
                    $"The artifact list '{list.Kind}' repeats movie id '{row.MovieId}'.",
                    nameof(list));
            }

            EnsureFiniteScore(row.Score, list.Kind);
            ValidateReasons(row.Reasons, list.Kind);
            ValidateDiagnostics(mode, row.Diagnostics, list.Kind);
        }
    }

    private static void EnsureFiniteScore(double score, string kind)
    {
        if (!double.IsFinite(score))
        {
            throw new ArgumentException($"The artifact list '{kind}' contains a non-finite score.", nameof(kind));
        }
    }

    private static void ValidateReasons(ImmutableArray<RecommendationArtifactReason> reasons, string kind)
    {
        if (reasons.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var reason in reasons)
        {
            if (reason.Kind is not (RecommendationArtifactContract.StructuralReasonKind or RecommendationArtifactContract.PersonalReasonKind))
            {
                throw new ArgumentException(
                    $"The artifact list '{kind}' carries an unrecognized reason kind '{reason.Kind}'.",
                    nameof(reasons));
            }

            if (reason.Text is null)
            {
                throw new ArgumentException($"The artifact list '{kind}' carries a reason without text.", nameof(reasons));
            }
        }
    }

    private static void ValidateDiagnostics(string mode, RecommendationPairDiagnostics? diagnostics, string kind)
    {
        if (diagnostics is null)
        {
            return;
        }

        if (mode != RecommendationArtifactContract.CompareMode)
        {
            throw new ArgumentException(
                $"The artifact list '{kind}' must not carry pair diagnostics outside compare mode.",
                nameof(kind));
        }

        foreach (var criterion in diagnostics.Breakdown)
        {
            EnsureFinite(criterion.Value, kind);
            EnsureFinite(criterion.Weight, kind);
            EnsureFinite(criterion.Contribution, kind);
        }

        if (diagnostics.RawPlotCosine is { } cosine)
        {
            EnsureFinite(cosine, kind);
        }

        if (diagnostics.PlotRank is <= 0)
        {
            throw new ArgumentException($"The artifact list '{kind}' plot rank must be positive when declared.", nameof(kind));
        }
    }

    private static void EnsureFinite(double value, string kind)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentException($"The artifact list '{kind}' carries a non-finite diagnostic number.", nameof(kind));
        }
    }

    private static bool IsKnownMode(string? mode) =>
        mode is RecommendationArtifactContract.CaptureMode or RecommendationArtifactContract.CompareMode;

    private static bool IsKnownListKind(string? kind) =>
        kind is RecommendationArtifactContract.ForYouListKind
            or RecommendationArtifactContract.RelatedListKind
            or RecommendationArtifactContract.CompareListKind;

    private static void EnsureObject(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new RecommendationArtifactFormatException("The artifact element must be a JSON object.");
        }
    }

    private static JsonElement RequiredProperty(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            throw new RecommendationArtifactFormatException($"The artifact is missing the '{name}' property.");
        }

        return property;
    }

    private static string ReadString(JsonElement element, string name)
    {
        var property = RequiredProperty(element, name);
        if (property.ValueKind != JsonValueKind.String)
        {
            throw new RecommendationArtifactFormatException($"The artifact property '{name}' must be a string.");
        }

        return property.GetString() ?? string.Empty;
    }

    private static int ReadInt32(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            throw new RecommendationArtifactFormatException($"The artifact property '{name}' must be an integer.");
        }

        return value;
    }

    private static int? ReadNullableInt32(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return ReadInt32(property, name);
    }

    private static double? ReadNullableDouble(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return ReadDouble(property, name);
    }

    private static double ReadDouble(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDouble(out var value) || !double.IsFinite(value))
        {
            throw new RecommendationArtifactFormatException($"The artifact property '{name}' must be a finite number.");
        }

        return value;
    }

    private static bool ReadBoolean(JsonElement element, string name)
    {
        var property = RequiredProperty(element, name);
        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new RecommendationArtifactFormatException($"The artifact property '{name}' must be a boolean."),
        };
    }
}
