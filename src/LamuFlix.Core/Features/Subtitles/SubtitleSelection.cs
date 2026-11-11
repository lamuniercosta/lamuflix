using System;
using System.Collections.Generic;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

public static class SubtitleSelection
{
    public static SubtitleCandidate? Select(
        IReadOnlyList<SubtitleCandidate> candidates,
        IReadOnlyList<string> preferredLanguages)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(preferredLanguages);

        foreach (var language in preferredLanguages)
        {
            var selected = BestOfLanguage(candidates, language);
            if (selected is not null)
            {
                return selected with { Language = language };
            }
        }

        return null;
    }

    private static SubtitleCandidate? BestOfLanguage(IReadOnlyList<SubtitleCandidate> candidates, string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        SubtitleCandidate? best = null;
        foreach (var candidate in candidates)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract // runtime guard for suppressed-null entries, covered by Select_NullCandidateEntry_IsSkipped
            if (candidate is null || !LanguageMatches(candidate.Language, language))
            {
                continue;
            }

            if (best is null || RanksBefore(candidate, best))
            {
                best = candidate;
            }
        }

        return best;
    }

    private static bool RanksBefore(SubtitleCandidate candidate, SubtitleCandidate incumbent)
    {
        if (candidate.MatchesMovieHash != incumbent.MatchesMovieHash)
        {
            return candidate.MatchesMovieHash;
        }

        return candidate.DownloadCount != incumbent.DownloadCount
            ? candidate.DownloadCount > incumbent.DownloadCount
            : candidate.FileId < incumbent.FileId;
    }

    private static bool LanguageMatches(string candidateLanguage, string preferred) =>
        string.Equals(candidateLanguage.Trim(), preferred.Trim(), StringComparison.OrdinalIgnoreCase);
}
