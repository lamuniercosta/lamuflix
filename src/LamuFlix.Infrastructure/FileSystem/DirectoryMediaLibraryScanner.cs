using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.FileSystem;

public sealed class DirectoryMediaLibraryScanner(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IOptions<LibraryOptions> libraryOptions)
    : IMediaLibraryScanner
{
    public ScannedMovie Scan(LibraryPath folder)
    {
        var (resolved, primary) = LocatePrimaryVideo(folder);
        var directory = fileSystem.DirectoryInfo.New(resolved.Value);
        var cleaned = FolderNameCleaner.Clean(directory.Name, timeProvider.GetUtcNow());

        return new ScannedMovie(resolved, cleaned.Title, new MediaFormat(primary.Extension), cleaned.Year, primary.Length);
    }

    public PrimaryVideo ResolvePrimaryVideo(LibraryPath folder)
    {
        var (resolved, primary) = LocatePrimaryVideo(folder);
        var path = new LibraryPath(fileSystem.Path.Combine(resolved.Value, primary.Name));

        return new PrimaryVideo(path, new MediaFormat(primary.Extension));
    }

    private (LibraryPath Resolved, IFileInfo Primary) LocatePrimaryVideo(LibraryPath folder)
    {
        var resolved = Resolve(folder);
        var directory = fileSystem.DirectoryInfo.New(resolved.Value);
        if (!directory.Exists)
        {
            throw new NotFoundException();
        }

        var primary = PrimaryVideoSelector.Select(directory)
            ?? throw new InvalidTransitionException(
                $"Library folder '{resolved.Value}' contains no eligible top-level video file.");

        return (resolved, primary);
    }

    private LibraryPath Resolve(LibraryPath folder)
    {
        if (fileSystem.Path.IsPathRooted(folder.Value))
        {
            return folder;
        }

        var relative = folder.Value.Trim('/', '\\');
        return new LibraryPath(fileSystem.Path.Combine(libraryOptions.Value.RootPath, relative));
    }
}

internal static class PrimaryVideoSelector
{
    private static readonly FrozenSet<string> ExcludedTokens =
        FrozenSet.ToFrozenSet(["sample", "trailer", "featurette", "extras"], StringComparer.OrdinalIgnoreCase);

    public static IFileInfo? Select(IDirectoryInfo directory) =>
        directory.EnumerateFiles()
            .Where(file => SupportedVideoExtensions.Contains(file.Extension))
            .Where(file => !IsExcluded(file.Name))
            .OrderByDescending(file => file.Length)
            .ThenBy(file => file.Name, StringComparer.Ordinal)
            .FirstOrDefault();

    private static bool IsExcluded(string fileName) =>
        Tokens(Path.GetFileNameWithoutExtension(fileName)).Any(ExcludedTokens.Contains);

    private static IEnumerable<string> Tokens(string stem)
    {
        var start = 0;
        for (var index = 0; index <= stem.Length; index++)
        {
            if (index < stem.Length && IsAsciiAlphanumeric(stem[index]))
            {
                continue;
            }

            if (index > start)
            {
                yield return stem[start..index];
            }

            start = index + 1;
        }
    }

    private static bool IsAsciiAlphanumeric(char value) =>
        value is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9');
}
