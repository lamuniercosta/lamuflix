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
    private const string RootIdentity = ".";

    public ScannedMovie Scan(LibraryPath folder)
    {
        var (resolved, identity, primary) = LocatePrimaryVideo(folder);
        var directory = fileSystem.DirectoryInfo.New(resolved);
        var cleaned = FolderNameCleaner.Clean(directory.Name, timeProvider.GetUtcNow());

        return new ScannedMovie(identity, cleaned.Title, new MediaFormat(primary.Extension), cleaned.Year, primary.Length);
    }

    public PrimaryVideo ResolvePrimaryVideo(LibraryPath folder)
    {
        var (resolved, _, primary) = LocatePrimaryVideo(folder);
        var path = new LibraryPath(fileSystem.Path.Combine(resolved, primary.Name));

        return new PrimaryVideo(path, new MediaFormat(primary.Extension));
    }

    private (string Resolved, LibraryPath Identity, IFileInfo Primary) LocatePrimaryVideo(LibraryPath folder)
    {
        var (resolved, identity) = Resolve(folder);
        var directory = fileSystem.DirectoryInfo.New(resolved);
        if (!directory.Exists)
        {
            throw new NotFoundException();
        }

        var primary = PrimaryVideoSelector.Select(directory)
            ?? throw new InvalidTransitionException(
                $"Library folder '{resolved}' contains no eligible top-level video file.");

        return (resolved, identity, primary);
    }

    /// <summary>
    /// Resolves a stored or submitted identity under the active <c>Library:RootPath</c> — never the
    /// process working directory — and reports the canonical library-relative identity it maps to.
    /// </summary>
    private (string Resolved, LibraryPath Identity) Resolve(LibraryPath folder)
    {
        var root = RequireRoot();
        if (!LibraryPath.TryCanonicalizeUnderRoot(folder.Value, root, out var canonical))
        {
            throw new NotFoundException();
        }

        var resolved = string.Equals(canonical, RootIdentity, StringComparison.Ordinal)
            ? root
            : fileSystem.Path.Combine(root, canonical);
        return (resolved, new LibraryPath(canonical));
    }

    private string RequireRoot()
    {
        var root = libraryOptions.Value.RootPath;
        if (!LibraryPath.IsAbsolute(root))
        {
            throw new InvalidOperationException(
                $"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)} must be an absolute library root; "
                + "relative paths are never resolved against the process working directory.");
        }

        return root;
    }
}

internal static class PrimaryVideoSelector
{
    private const string SampleToken = "sample";
    private const string TrailerToken = "trailer";
    private const string FeaturetteToken = "featurette";
    private const string ExtrasToken = "extras";

    private static readonly FrozenSet<string> ExcludedTokens =
        FrozenSet.ToFrozenSet([SampleToken, TrailerToken, FeaturetteToken, ExtrasToken], StringComparer.OrdinalIgnoreCase);

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
