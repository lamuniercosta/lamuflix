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
/// Streams Candidate Movie Folders under the configured library root as canonical library-relative
/// identities (the root itself is <c>.</c>) using an explicit stack of lazy enumerators: no
/// call-stack recursion, no depth cap and no materialized child/file lists.
/// </summary>
public sealed class FileSystemLibraryFolderDiscovery(
    IFileSystem fileSystem,
    IOptions<LibraryOptions> libraryOptions)
    : ILibraryFolderDiscovery
{
    private const string RootIdentity = ".";
    private static readonly char[] Separators = ['/', '\\'];

    // ReSharper disable once AsyncMethodWithoutAwait - async is required for the iterator and EnumeratorCancellation; the filesystem enumerators are synchronous by design.
    public async IAsyncEnumerable<LibraryPath> DiscoverAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var root = ResolveRoot();
        var rootInfo = RequireRootDirectory(root);

        var comparison = HostComparison();
        var pending = new Stack<IEnumerator<IFileSystemInfo>>();
        try
        {
            if (HasSupportedVideo(rootInfo))
            {
                yield return new LibraryPath(RootIdentity);
            }

            pending.Push(rootInfo.EnumerateFileSystemInfos().GetEnumerator());
            while (TryTakeNext(pending, out var entry))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry is not IDirectoryInfo child || IsExcluded(child, root, comparison))
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
            Drain(pending);
        }
    }

    private static bool TryTakeNext(Stack<IEnumerator<IFileSystemInfo>> pending, out IFileSystemInfo? entry)
    {
        while (pending.Count > 0)
        {
            var current = pending.Peek();
            bool advanced;
            try
            {
                advanced = current.MoveNext();
            }
            catch
            {
                Drop(pending);
                throw;
            }

            if (!advanced)
            {
                Drop(pending);
                continue;
            }

            entry = current.Current;
            return true;
        }

        entry = null;
        return false;
    }

    private static bool IsExcluded(IDirectoryInfo child, string root, StringComparison comparison) =>
        IsLink(child) || !IsContained(root, child.FullName, comparison);

    private static void Drop(Stack<IEnumerator<IFileSystemInfo>> pending) =>
        pending.Pop().Dispose();

    private static void Drain(Stack<IEnumerator<IFileSystemInfo>> pending)
    {
        while (pending.Count > 0)
        {
            Drop(pending);
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

    private IDirectoryInfo RequireRootDirectory(string root)
    {
        var rootInfo = fileSystem.DirectoryInfo.New(root);
        if (!rootInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Library root '{root}' does not exist.");
        }

        return rootInfo;
    }

    /// <summary>
    /// Resolves the configured root, refusing a non-absolute value so discovery can never silently
    /// enumerate relative to the process working directory.
    /// </summary>
    private string ResolveRoot()
    {
        var configured = libraryOptions.Value.RootPath;
        if (!LibraryPath.IsAbsolute(configured))
        {
            throw new InvalidOperationException(
                $"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)} must be an absolute library root; "
                + "relative roots are never resolved against the process working directory.");
        }

        var info = fileSystem.DirectoryInfo.New(configured);
        var resolved = info.LinkTarget is null
            ? info.FullName
            : info.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? info.FullName;
        return TrimTrailingSeparators(resolved);
    }

    private LibraryPath CandidatePath(string root, string fullName)
    {
        var relative = fileSystem.Path.GetRelativePath(root, fullName).Trim(Separators);
        if (relative.Length == 0 || relative == ".")
        {
            return new LibraryPath(RootIdentity);
        }

        return new LibraryPath(string.Join('/', relative.Split(Separators, StringSplitOptions.RemoveEmptyEntries)));
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
        if (HasUncPrefix(path))
        {
            return UncRootLength(path);
        }

        if (HasDrivePrefix(path))
        {
            return 3;
        }

        return SingleRootLength(path);
    }

    private static bool HasUncPrefix(string path) =>
        path.Length >= 2 && IsSeparator(path[0]) && IsSeparator(path[1]);

    private static bool HasDrivePrefix(string path) =>
        path.Length >= 3 && path[1] == ':' && IsSeparator(path[2]);

    private static int SingleRootLength(string path) =>
        path.Length >= 1 && IsSeparator(path[0]) ? 1 : 0;

    private static int UncRootLength(string path)
    {
        var shareStart = IndexOfSeparator(path, 2);
        if (shareStart < 0)
        {
            return path.Length;
        }

        var shareEnd = IndexOfSeparator(path, shareStart + 1);
        return shareEnd < 0 ? path.Length : shareEnd + 1;
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
        character == '/' || character == '\\';
}
