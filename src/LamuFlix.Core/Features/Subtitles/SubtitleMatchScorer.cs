using System;
using System.Collections.Generic;
using System.Text;

namespace LamuFlix.Core.Features.Subtitles;

public static class SubtitleMatchScorer
{
    public const int MovieHashMatchPercent = 100;
    public const int NoOverlapPercent = 0;

    public static int Score(string? videoStem, string? releaseName, bool matchesMovieHash)
    {
        if (matchesMovieHash)
        {
            return MovieHashMatchPercent;
        }

        var videoTokens = DistinctTokens(videoStem);
        var releaseTokens = DistinctTokens(releaseName);
        if (videoTokens.Count == 0 || releaseTokens.Count == 0)
        {
            return NoOverlapPercent;
        }

        var intersection = 0;
        foreach (var token in releaseTokens)
        {
            if (videoTokens.Contains(token))
            {
                intersection++;
            }
        }

        var union = videoTokens.Count + releaseTokens.Count - intersection;
        return union == 0
            ? NoOverlapPercent
            : (int)Math.Round(100d * intersection / union, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlySet<string> DistinctTokens(string? value)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(value))
        {
            return tokens;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (IsTokenCharacter(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            Flush(tokens, builder);
        }

        Flush(tokens, builder);
        return tokens;
    }

    public static int Compare(string? firstReleaseName, string firstCandidateId, string? secondReleaseName, string secondCandidateId)
    {
        var release = string.CompareOrdinal(firstReleaseName ?? string.Empty, secondReleaseName ?? string.Empty);
        return release != 0
            ? release
            : string.CompareOrdinal(firstCandidateId, secondCandidateId);
    }

    private static void Flush(HashSet<string> tokens, StringBuilder builder)
    {
        if (builder.Length == 0)
        {
            return;
        }

        tokens.Add(builder.ToString());
        builder.Clear();
    }

    private static bool IsTokenCharacter(char character) =>
        char.IsLetter(character) || char.IsDigit(character);
}
