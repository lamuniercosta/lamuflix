using System;
using System.Collections.Frozen;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.FileSystem;

public sealed class DirectoryMediaLibraryScanner(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IOptions<LibraryOptions> libraryOptions)
    : IMediaLibraryScanner
{
    private static readonly FrozenSet<string> SupportedExtensions =
        FrozenSet.ToFrozenSet([".mkv", ".mp4", ".avi", ".m4v", ".rmvb", ".rm", ".flv"], StringComparer.OrdinalIgnoreCase);

    public ScannedMovie Scan(LibraryPath folder)
    {
        var resolved = Resolve(folder);
        var directory = fileSystem.DirectoryInfo.New(resolved.Value);
        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"Library folder '{resolved.Value}' does not exist.");
        }

        var primary = SelectPrimaryVideo(directory);
        if (primary is null)
        {
            throw new InvalidOperationException(
                $"Library folder '{resolved.Value}' contains no supported top-level video file.");
        }

        var cleaned = FolderNameCleaner.Clean(directory.Name, timeProvider.GetUtcNow());

        return new ScannedMovie(resolved, cleaned.Title, new MediaFormat(primary.Extension), cleaned.Year, primary.Length);
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

    private static IFileInfo? SelectPrimaryVideo(IDirectoryInfo directory) =>
        directory.EnumerateFiles()
            .Where(file => SupportedExtensions.Contains(file.Extension))
            .OrderByDescending(file => file.Length)
            .ThenBy(file => file.Name, StringComparer.Ordinal)
            .FirstOrDefault();

}
