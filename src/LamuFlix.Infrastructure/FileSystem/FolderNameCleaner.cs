using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.FileSystem;

/// <summary>
/// Recovers a movie title and release year from a library folder name such as
/// <c>Title (2010)</c>, <c>Title[2010]</c>, <c>The.Matrix.1999.1080p.BluRay.x264-GROUP</c> or
/// <c>www.site.com - Title (2010) [1080p] [WEBRip]</c>.
/// </summary>
public static partial class FolderNameCleaner
{
    private static readonly char[] TagSeparators = [' ', '.', '_'];

    /// <summary>
    /// Cleans <paramref name="folderName"/>. The last year that is plausible at <paramref name="now"/>
    /// and followed only by tags, by a release tag such as a resolution or source, or by the end of the
    /// name is taken as the release year; everything from that year onwards is discarded. A trailing
    /// edition label such as <c>EXTENDED</c> is then dropped and runs of whitespace are collapsed.
    /// Without such a year the name is otherwise returned unchanged.
    /// </summary>
    public static CleanedFolderName Clean(string folderName, DateTimeOffset now)
    {
        var trimmed = folderName.Trim();
        var stripped = SitePrefix().Replace(trimmed, string.Empty).Trim();
        var name = stripped.Length == 0 ? trimmed : stripped;
        var text = IsSceneName(name) ? name.Replace('.', ' ').Replace('_', ' ') : name;

        var year = LastReleaseYear(text, now);
        if (year is null)
        {
            return new CleanedFolderName(Tidy(text), null);
        }

        var title = TrimTitle(text[..year.Value.Index]);
        return new CleanedFolderName(Tidy(title.Length == 0 ? text : title), year.Value.Year);
    }

    private static bool IsSceneName(string name) =>
        name.All(character => !char.IsWhiteSpace(character)) && name.AsSpan().IndexOfAny('.', '_') >= 0;

    private static (int Index, ReleaseYear Year)? LastReleaseYear(string text, DateTimeOffset now)
    {
        foreach (var candidate in YearPattern().Matches(text).Reverse())
        {
            if (!ReleaseYear.TryCreate(int.Parse(candidate.Groups["year"].Value, CultureInfo.InvariantCulture), now, out var year))
            {
                continue;
            }

            var tail = text[(candidate.Index + candidate.Length)..];
            if (TailIsOnlyTags(tail) || TailStartsWithReleaseTag(tail))
            {
                return (candidate.Index, year);
            }
        }

        return null;
    }

    private static bool TailIsOnlyTags(string tail) =>
        TailTokens(tail).All(token => TagPattern().IsMatch(token));

    /// <summary>
    /// A year directly followed by a resolution, source or codec is a release year even when unknown
    /// tokens such as a release group follow, as in <c>Title 2024 1080p WEBRip 10bit-GalaxyRG265</c>.
    /// </summary>
    private static bool TailStartsWithReleaseTag(string tail) =>
        TailTokens(tail).FirstOrDefault() is { } first && ReleaseTagPattern().IsMatch(first);

    private static IEnumerable<string> TailTokens(string tail) =>
        GroupPattern().Replace(tail, " ")
            .Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token != "-");

    private static string TrimTitle(string title) => title.Trim(' ', '.', '-', '_');

    private static string Tidy(string title)
    {
        var collapsed = Whitespace().Replace(title.Trim(), " ");
        var withoutEdition = EditionSuffix().Replace(collapsed, string.Empty);
        return withoutEdition.Length == 0 ? collapsed : TrimTitle(withoutEdition);
    }

    [GeneratedRegex(@"^www\.\S+\s+-\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SitePrefix();

    [GeneratedRegex(
        @"\((?<year>[0-9]{4})\)|\[(?<year>[0-9]{4})\]|(?<=^|[\s._])(?<year>[0-9]{4})(?=$|[\s._\[(])",
        RegexOptions.CultureInvariant)]
    private static partial Regex YearPattern();

    [GeneratedRegex(@"\[[^\]]*\]|\([^)]*\)", RegexOptions.CultureInvariant)]
    private static partial Regex GroupPattern();

    [GeneratedRegex(
        @"^(?:[0-9]{3,4}p|4k|8k|uhd|hdr10?|hdr|sdr|dv|dolby|x26[45]|h26[45]|hevc|avc|xvid|divx|aac[0-9]*|ac3|eac3|ddp?[0-9]*|dts(?:-?hd)?|truehd|atmos|[0-9]|[0-9]+ch|(?:8|10|12)bit|blu-?ray|bdrip|brrip|bd|webrip|web|hdtv|hdrip|dvdrip|dvd|dvdscr|screener|hdcam|cam|camrip|hdts|ts|telesync|tc|remux|proper|repack|extended|unrated|uncut|remastered|theatrical|imax|yts|rarbg|ettv|eztv|retail|internal|limited|multi|dual|subbed|dubbed|amzn|nf|dsnp|hmax|hulu|max)(?:-[a-z0-9]+)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();

    [GeneratedRegex(
        @"^(?:[0-9]{3,4}p|4k|uhd|blu-?ray|bdrip|brrip|webrip|web|hdtv|hdrip|dvdrip|dvdscr|hdcam|camrip|hdts|telesync|remux|x26[45]|h26[45]|hevc|xvid)(?:-[a-z0-9]+)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReleaseTagPattern();

    [GeneratedRegex(
        @"\s+(?:extended(?:\s+(?:edition|cut|version))?|director'?s\s+cut|unrated(?:\s+(?:edition|cut|version))?|uncut|remastered|theatrical(?:\s+(?:cut|version))?|special\s+edition)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EditionSuffix();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
