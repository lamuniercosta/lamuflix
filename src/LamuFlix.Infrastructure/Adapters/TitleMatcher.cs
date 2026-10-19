using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LamuFlix.Infrastructure.Adapters;

/// <summary>
/// Scores how well a provider search candidate matches a requested movie title. Pure and dependency
/// free: titles are normalised (case, diacritics, punctuation), compared with a Levenshtein ratio and
/// a token overlap, and tried with bracket groups removed, small digits spelled as words and tens
/// written as words turned into digits.
/// </summary>
public static partial class TitleMatcher
{
    /// <summary>Added to the similarity when both years are known and differ by at most one year.</summary>
    private const double YearBonus = 0.1;

    /// <summary>Largest year difference that is still considered the same release.</summary>
    private const int YearTolerance = 1;

    /// <summary>
    /// Most years a candidate may come out after the requested year. OMDb sometimes lists the later
    /// US release, e.g. City of God (2002) as 2004 and A Fistful of Dollars (1964) as 1967.
    /// </summary>
    private const int LateReleaseTolerance = 3;

    /// <summary>Subtracted for each year a late candidate lies beyond <see cref="YearTolerance"/>.</summary>
    private const double LateReleasePenalty = 0.05;

    /// <summary>Subtracted when only the candidate's main title, before its last " - ", matches.</summary>
    private const double SubtitlePenalty = 0.05;

    private const string DashSeparator = " - ";

    private const int MaxSpelledNumber = 20;

    private static readonly string[] NumberWords =
    [
        "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen", "Twenty",
    ];

    private static readonly string[] TensWords = ["Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];

    /// <summary>
    /// Returns the score of a candidate in the range 0 to 1, or <c>null</c> when both years are known and
    /// the candidate is more than <see cref="YearTolerance"/> years earlier or more than
    /// <see cref="LateReleaseTolerance"/> years later. A candidate within one year earns
    /// <see cref="YearBonus"/>; a later one loses <see cref="LateReleasePenalty"/> per extra year.
    /// </summary>
    public static double? Score(string queryTitle, int? queryYear, string candidateTitle, int? candidateYear) =>
        YearAdjustment(queryYear, candidateYear) is { } adjustment
            ? Math.Clamp(TitleSimilarity(queryTitle, candidateTitle) + adjustment, 0.0, 1.0)
            : null;

    /// <summary>The best title similarity from 0 to 1 across the normalised variants of both titles.</summary>
    public static double Similarity(string left, string right)
    {
        var best = 0.0;
        foreach (var leftForm in Forms(left))
        {
            foreach (var rightForm in Forms(right))
            {
                best = Math.Max(best, Compare(leftForm, rightForm));
            }
        }

        return best;
    }

    /// <summary>
    /// Title variants used to search a provider, in order: the title with bracket groups removed, the same
    /// with " - " written as ": " (OMDb search finds "Futurama: Into the Wild Green Yonder" but not the
    /// dashed form), with digits from zero to twenty spelled as words, and with tens from twenty to
    /// ninety-nine written as words turned into digits. Duplicates and empty variants are dropped.
    /// </summary>
    public static IReadOnlyList<string> SearchVariants(string title)
    {
        var stripped = StripGroups(title);
        var baseTitle = stripped.Length == 0 ? title.Trim() : stripped;
        return Distinct(
            baseTitle,
            baseTitle.Replace(DashSeparator, ": ", StringComparison.Ordinal),
            SpellNumbers(baseTitle),
            DigitizeTens(baseTitle));
    }

    private static double? YearAdjustment(int? queryYear, int? candidateYear)
    {
        if (queryYear is not { } query || candidateYear is not { } candidate)
        {
            return 0.0;
        }

        var lateBy = candidate - query;
        return lateBy switch
        {
            >= -YearTolerance and <= YearTolerance => YearBonus,
            > YearTolerance and <= LateReleaseTolerance => -LateReleasePenalty * (lateBy - YearTolerance),
            _ => null,
        };
    }

    private static double TitleSimilarity(string queryTitle, string candidateTitle)
    {
        var full = Similarity(queryTitle, candidateTitle);
        var separator = candidateTitle.LastIndexOf(DashSeparator, StringComparison.Ordinal);
        return separator <= 0
            ? full
            : Math.Max(full, Similarity(queryTitle, candidateTitle[..separator]) - SubtitlePenalty);
    }

    private static IEnumerable<string> Forms(string title)
    {
        var stripped = StripGroups(title);
        var variants = Distinct(
            title,
            stripped,
            SpellNumbers(title),
            SpellNumbers(stripped),
            DigitizeTens(title),
            DigitizeTens(stripped));
        return variants.Select(Normalize).Where(form => form.Length > 0).Distinct(StringComparer.Ordinal);
    }

    private static double Compare(string left, string right) =>
        Math.Max(LevenshteinRatio(left, right), TokenOverlap(left, right));

    private static double LevenshteinRatio(string left, string right)
    {
        var longest = Math.Max(left.Length, right.Length);
        if (longest == 0)
        {
            return 1.0;
        }

        return 1.0 - (double)Distance(left, right) / longest;
    }

    private static int Distance(string left, string right)
    {
        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        var current = new int[right.Length + 1];
        for (var row = 1; row <= left.Length; row++)
        {
            current[0] = row;
            for (var column = 1; column <= right.Length; column++)
            {
                var substitution = previous[column - 1] + (left[row - 1] == right[column - 1] ? 0 : 1);
                current[column] = Math.Min(Math.Min(current[column - 1] + 1, previous[column] + 1), substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static double TokenOverlap(string left, string right)
    {
        var leftTokens = new HashSet<string>(left.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        var rightTokens = new HashSet<string>(right.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        var union = leftTokens.Count + rightTokens.Count;
        if (union == 0)
        {
            return 0.0;
        }

        var shared = leftTokens.Count(rightTokens.Contains);
        return (double)shared / (union - shared);
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (character == '&')
            {
                builder.Append(" and ");
            }
            else if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else
            {
                builder.Append(' ');
            }
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string StripGroups(string title) =>
        GroupPattern().Replace(title, " ").Trim();

    private static string SpellNumbers(string title) =>
        NumberPattern().Replace(
            title,
            match => int.Parse(match.Value, CultureInfo.InvariantCulture) is var number && number <= MaxSpelledNumber
                ? NumberWords[number]
                : match.Value);

    private static string DigitizeTens(string title) =>
        TensPattern().Replace(
            title,
            match => (TensValue(match.Groups["tens"].Value) + OnesValue(match.Groups["ones"])).ToString(CultureInfo.InvariantCulture));

    private static int TensValue(string word) =>
        (Array.FindIndex(TensWords, tens => tens.Equals(word, StringComparison.OrdinalIgnoreCase)) + 2) * 10;

    private static int OnesValue(Group ones) =>
        ones.Success ? Array.FindIndex(NumberWords, word => word.Equals(ones.Value, StringComparison.OrdinalIgnoreCase)) : 0;

    private static IReadOnlyList<string> Distinct(params string[] values) =>
        [.. values.Where(value => value.Trim().Length > 0).Distinct(StringComparer.Ordinal)];

    [GeneratedRegex(@"\[[^\]]*\]|\([^)]*\)", RegexOptions.CultureInvariant)]
    private static partial Regex GroupPattern();

    [GeneratedRegex(@"(?<![0-9A-Za-z])[0-9]{1,2}(?![0-9A-Za-z])", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();

    [GeneratedRegex(
        @"\b(?<tens>twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety)(?:[- ](?<ones>one|two|three|four|five|six|seven|eight|nine)\b)?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TensPattern();
}
