using System;
using System.Collections.Frozen;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Infrastructure.FileSystem;

public sealed partial class DirectoryMediaLibraryScanner(IFileSystem fileSystem, TimeProvider timeProvider)
    : IMediaLibraryScanner
{
    private static readonly FrozenSet<string> SupportedExtensions =
        FrozenSet.ToFrozenSet([".mkv", ".mp4", ".avi", ".m4v"], StringComparer.OrdinalIgnoreCase);

    public ScannedMovie Scan(LibraryPath folder)
    {
        var directory = fileSystem.DirectoryInfo.New(folder.Value);
        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"Library folder '{folder.Value}' does not exist.");
        }

        var primary = SelectPrimaryVideo(directory);
        if (primary is null)
        {
            throw new InvalidOperationException(
                $"Library folder '{folder.Value}' contains no supported top-level video file.");
        }

        var (title, year) = ParseFolderName(directory.Name, timeProvider.GetUtcNow());

        return new ScannedMovie(folder, title, new MediaFormat(primary.Extension), year, primary.Length);
    }

    private static IFileInfo? SelectPrimaryVideo(IDirectoryInfo directory) =>
        directory.EnumerateFiles()
            .Where(file => SupportedExtensions.Contains(file.Extension))
            .OrderByDescending(file => file.Length)
            .ThenBy(file => file.Name, StringComparer.Ordinal)
            .FirstOrDefault();

    private static (string Title, ReleaseYear? Year) ParseFolderName(string name, DateTimeOffset now)
    {
        var trimmed = name.Trim();
        var match = FolderNamePattern().Match(trimmed);
        if (!match.Success ||
            !ReleaseYear.TryCreate(int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture), now, out var year))
        {
            return (trimmed, null);
        }

        return string.IsNullOrWhiteSpace(match.Groups["title"].Value)
            ? (trimmed, year)
            : (match.Groups["title"].Value, year);
    }

    [GeneratedRegex(@"^(?<title>.*?)\s*(?:\((?<year>[0-9]{4})\)|\[(?<year>[0-9]{4})\])$",
        RegexOptions.CultureInvariant)]
    private static partial Regex FolderNamePattern();
}
