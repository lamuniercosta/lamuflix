using System.Collections.Generic;

namespace LamuFlix.Core.Features.Recommendations;

internal static class OutsideLibraryReasonFormatter
{
    private const string Prefix = "Recommended for ";
    private const string Joiner = " and ";

    internal static string Format(IReadOnlyList<string> seedTitles) =>
        seedTitles.Count < 2
            ? string.Empty
            : string.Concat(
                Prefix,
                SimilarityScorer.SanitizeDisplayText(seedTitles[0]),
                Joiner,
                SimilarityScorer.SanitizeDisplayText(seedTitles[1]));
}
