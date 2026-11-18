using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LamuFlix.Evaluation.RealLibrary;

public static class JudgedPairReader
{
    public static ImmutableArray<JudgedPair> ReadOrDefault(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);

        return File.Exists(fullPath)
            ? ReadFromUtf8Bytes(File.ReadAllBytes(fullPath))
            : ImmutableArray<JudgedPair>.Empty;
    }

    public static ImmutableArray<JudgedPair> ReadFromUtf8Bytes(ReadOnlySpan<byte> json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json.ToArray());
        }
        catch (JsonException exception)
        {
            throw new JudgedPairsFormatException("The judged-pairs file is not valid JSON.", exception);
        }

        using (document)
        {
            return ReadRoot(document.RootElement);
        }
    }

    private static ImmutableArray<JudgedPair> ReadRoot(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JudgedPairsFormatException(
                "The judged-pairs file must contain a JSON array of directed entries.");
        }

        var pairs = ImmutableArray.CreateBuilder<JudgedPair>();
        var seen = new HashSet<(int, int)>();

        foreach (var entry in root.EnumerateArray())
        {
            var pair = ReadEntry(entry);

            if (!seen.Add((pair.MovieId, pair.OtherId)))
            {
                throw new JudgedPairsFormatException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Duplicate directed judged pair {0} -> {1}.",
                        pair.MovieId,
                        pair.OtherId));
            }

            pairs.Add(pair);
        }

        return pairs.ToImmutable();
    }

    private static JudgedPair ReadEntry(JsonElement entry)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            throw new JudgedPairsFormatException("Every judged-pairs entry must be a JSON object.");
        }

        var movieId = ReadId(entry, "movieId");
        var otherId = ReadId(entry, "otherId");
        var verdict = ReadVerdict(entry);
        var note = ReadNote(entry);

        if (movieId == otherId)
        {
            throw new JudgedPairsFormatException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "A judged pair must not point at itself; got {0}.",
                    movieId));
        }

        return new JudgedPair(movieId, otherId, verdict, note);
    }

    private static int ReadId(JsonElement entry, string name)
    {
        if (!entry.TryGetProperty(name, out var element)
            || element.ValueKind != JsonValueKind.Number
            || !element.TryGetInt32(out var value)
            || value <= 0)
        {
            throw new JudgedPairsFormatException(
                $"Every judged-pairs entry needs a positive integer '{name}'.");
        }

        return value;
    }

    private static JudgedVerdict ReadVerdict(JsonElement entry)
    {
        if (!entry.TryGetProperty("verdict", out var element) || element.ValueKind != JsonValueKind.String)
        {
            throw new JudgedPairsFormatException(
                "Every judged-pairs entry needs a string 'verdict' of 'good' or 'bad'.");
        }

        return element.GetString() switch
        {
            "good" => JudgedVerdict.Good,
            "bad" => JudgedVerdict.Bad,
            var other => throw new JudgedPairsFormatException(
                $"The judged-pairs verdict must be 'good' or 'bad'; got '{other}'."),
        };
    }

    private static string? ReadNote(JsonElement entry)
    {
        if (!entry.TryGetProperty("note", out var element))
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.String)
        {
            return element.GetString();
        }

        return element.ValueKind is JsonValueKind.Null
            ? null
            : throw new JudgedPairsFormatException("The optional judged-pairs 'note' must be a string.");
    }
}

public enum JudgedVerdict
{
    Good,
    Bad,
}

public sealed record JudgedPair(int MovieId, int OtherId, JudgedVerdict Verdict, string? Note);

public sealed class JudgedPairsFormatException : Exception
{
    public JudgedPairsFormatException(string message)
        : base(message)
    {
    }

    public JudgedPairsFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public static class JudgedSharesCalculator
{
    public static JudgedShares Compute(
        ImmutableArray<JudgedPairOutcome> outcomes,
        ImmutableArray<double> bars,
        JudgedVerdict verdict,
        bool includeRankOne)
    {
        var valid = outcomes
            .Where(outcome => outcome.Pair.Verdict == verdict)
            .ToImmutableArray();

        if (valid.IsEmpty)
        {
            return new JudgedShares(
                0,
                null,
                null,
                null,
                [.. bars.Select(bar => new BarShare(bar, null))]);
        }

        var total = (double)valid.Length;

        return new JudgedShares(
            valid.Length,
            includeRankOne ? valid.Count(outcome => outcome.IsRankOne) / total : null,
            valid.Count(outcome => outcome.IsTopThree) / total,
            valid.Count(outcome => outcome.IsStrong) / total,
            [
                .. bars.Select(bar => new BarShare(
                    bar,
                    valid.Count(outcome => outcome.BarsMet.Contains(bar)) / total)),
            ]);
    }
}
