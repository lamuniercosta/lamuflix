using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.FileSystem;

/// <summary>
/// Streams Candidate Movie Folders under the configured library root using an explicit stack of
/// lazy enumerators: no call-stack recursion, no depth cap and no materialized child/file lists.
/// </summary>
public sealed class FileSystemLibraryFolderDiscovery(
    IFileSystem fileSystem,
    IOptions<LibraryOptions> libraryOptions)
    : ILibraryFolderDiscovery
{
    private static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public async IAsyncEnumerable<LibraryPath> DiscoverAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var root = ResolveRoot();
        var rootInfo = fileSystem.DirectoryInfo.New(root);
        if (!rootInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Library root '{root}' does not exist.");
        }

        var comparison = HostComparison();
        var pending = new Stack<IEnumerator<IFileSystemInfo>>();
        try
        {
            if (HasSupportedVideo(rootInfo))
            {
                yield return new LibraryPath(root);
            }

            pending.Push(rootInfo.EnumerateFileSystemInfos().GetEnumerator());
            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pending.Peek();
                bool advanced;
                try
                {
                    advanced = current.MoveNext();
                }
                catch
                {
                    current.Dispose();
                    pending.Pop();
                    throw;
                }

                if (!advanced)
                {
                    current.Dispose();
                    pending.Pop();
                    continue;
                }

                if (current.Current is not IDirectoryInfo child
                    || IsLink(child)
                    || !IsContained(root, child.FullName, comparison))
                {
                    continue;
                }

                if (HasSupportedVideo(child))
                {
                    yield return CandidatePath(root, child.FullName);
                }

                pending.Push(child.EnumerateFileSystemInfos().GetEnumerator());
            }
        }
        finally
        {
            while (pending.Count > 0)
            {
                pending.Pop().Dispose();
            }
        }
    }

    /// <summary>Windows compares library paths case-insensitively; every other supported host is ordinal.</summary>
    internal static StringComparison HostComparison() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    /// <summary>
    /// True when <paramref name="candidate"/> is <paramref name="root"/> or sits below its directory
    /// boundary. Filesystem roots (<c>/</c>, <c>C:\</c>) keep their own length so the boundary survives.
    /// </summary>
    internal static bool IsContained(string root, string candidate, StringComparison comparison)
    {
        var boundary = TrimTrailingSeparators(root);
        if (boundary.Length == 0)
        {
            return false;
        }

        if (string.Equals(boundary, candidate, comparison))
        {
            return true;
        }

        if (!candidate.StartsWith(boundary, comparison))
        {
            return false;
        }

        return IsSeparator(boundary[^1]) || IsSeparator(candidate[boundary.Length]);
    }

    private string ResolveRoot()
    {
        var info = fileSystem.DirectoryInfo.New(libraryOptions.Value.RootPath);
        var resolved = info.LinkTarget is null
            ? info.FullName
            : info.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? info.FullName;
        return TrimTrailingSeparators(resolved);
    }

    private LibraryPath CandidatePath(string root, string fullName)
    {
        var relative = fileSystem.Path.GetRelativePath(root, fullName).Trim(Separators);
        return new LibraryPath(relative.Length == 0 || relative == "." ? root : relative);
    }

    private static bool HasSupportedVideo(IDirectoryInfo directory) =>
        directory.EnumerateFiles().Any(file => SupportedVideoExtensions.Contains(file.Extension));

    private static bool IsLink(IFileSystemInfo info) =>
        info.LinkTarget is not null || (info.Attributes & FileAttributes.ReparsePoint) != 0;

    private static string TrimTrailingSeparators(string path)
    {
        var rootLength = RootLength(path);
        var end = path.Length;
        while (end > rootLength && IsSeparator(path[end - 1]))
        {
            end--;
        }

        return path[..end];
    }

    private static int RootLength(string path)
    {
        if (path.Length >= 2 && IsSeparator(path[0]) && IsSeparator(path[1]))
        {
            var shareStart = IndexOfSeparator(path, 2);
            if (shareStart < 0)
            {
                return path.Length;
            }

            var shareEnd = IndexOfSeparator(path, shareStart + 1);
            return shareEnd < 0 ? path.Length : shareEnd + 1;
        }

        if (path.Length >= 3 && path[1] == ':' && IsSeparator(path[2]))
        {
            return 3;
        }

        return path.Length >= 1 && IsSeparator(path[0]) ? 1 : 0;
    }

    private static int IndexOfSeparator(string path, int start)
    {
        for (var index = start; index < path.Length; index++)
        {
            if (IsSeparator(path[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsSeparator(char character) =>
        character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar;
}
