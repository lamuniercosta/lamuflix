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
/// a token overlap, and tried with bracket groups removed and digits spelled as words.
/// </summary>
public static partial class TitleMatcher
{
    /// <summary>Added to the similarity when both years are known and differ by at most one year.</summary>
    private const double YearBonus = 0.1;

    /// <summary>Largest year difference that is still considered the same release.</summary>
    private const int YearTolerance = 1;

    private const int MaxSpelledNumber = 20;

    private static readonly string[] NumberWords =
    [
        "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen", "Twenty",
    ];

    /// <summary>
    /// Returns the score of a candidate in the range 0 to 1, or <c>null</c> when the candidate's year is
    /// known on both sides and differs by more than <see cref="YearTolerance"/>.
    /// </summary>
    public static double? Score(string queryTitle, int? queryYear, string candidateTitle, int? candidateYear)
    {
        var yearBonus = 0.0;
        if (queryYear is { } query && candidateYear is { } candidate)
        {
            var difference = Math.Abs(query - candidate);
            if (difference > YearTolerance)
            {
                return null;
            }

            yearBonus = YearBonus;
        }

        return Math.Min(1.0, Similarity(queryTitle, candidateTitle) + yearBonus);
    }

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
    /// Title variants used to search a provider: the title with bracket groups removed, and the same with
    /// digits from zero to twenty spelled as words. Duplicates and empty variants are dropped.
    /// </summary>
    public static IReadOnlyList<string> SearchVariants(string title)
    {
        var stripped = StripGroups(title);
        var baseTitle = stripped.Length == 0 ? title.Trim() : stripped;
        return Distinct(baseTitle, SpellNumbers(baseTitle));
    }

    private static IEnumerable<string> Forms(string title)
    {
        var variants = Distinct(title, StripGroups(title), SpellNumbers(title), SpellNumbers(StripGroups(title)));
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

    private static IReadOnlyList<string> Distinct(params string[] values) =>
        [.. values.Where(value => value.Trim().Length > 0).Distinct(StringComparer.Ordinal)];

    [GeneratedRegex(@"\[[^\]]*\]|\([^)]*\)", RegexOptions.CultureInvariant)]
    private static partial Regex GroupPattern();

    [GeneratedRegex(@"(?<![0-9A-Za-z])[0-9]{1,2}(?![0-9A-Za-z])", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();
}
