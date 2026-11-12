using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.FileSystem;

public sealed partial class LibraryFileReader(
    IOptions<LibraryOptions> libraryOptions,
    IHostEnvironment hostEnvironment) : ILibraryFileReader
{
    private const int StreamBufferSize = 81920;

    public void AuthorizeFolder(LibraryPath folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var authorized = AuthorizeExisting(folder.Value);
        if (!Directory.Exists(authorized))
        {
            throw Denied();
        }
    }

    public Stream OpenRead(LibraryPath file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var authorized = AuthorizeExisting(file.Value);
        if (Directory.Exists(authorized))
        {
            throw Denied();
        }

        RejectNonRegularFile(authorized);

        var stream = OpenAuthorized(authorized);
        try
        {
            VerifyOpenedStream(stream);
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public IReadOnlyList<string> EnumerateFiles(LibraryPath folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var authorized = AuthorizeExisting(folder.Value);
        if (!Directory.Exists(authorized))
        {
            throw Denied();
        }

        return EnumerateRegularFiles(authorized);
    }

    private const string SubsDirectoryName = "Subs";

    private static IReadOnlyList<string> EnumerateRegularFiles(string authorizedDirectory)
    {
        var names = new List<string>();
        var subs = new List<string>();
        foreach (var candidate in Directory.EnumerateFileSystemEntries(authorizedDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            if (IsRegularFileEntry(candidate))
            {
                names.Add(Path.GetFileName(candidate));
            }
            else if (IsSubsDirectory(candidate, authorizedDirectory))
            {
                subs.Add(candidate);
            }
        }

        names.AddRange(ReadSubsFiles(SelectSubsDirectory(subs)));

        names.Sort(StringComparer.Ordinal);
        return names;
    }

    private static bool IsSubsDirectory(string candidate, string authorizedDirectory)
    {
        try
        {
            if (!string.Equals(
                Path.GetFileName(candidate), SubsDirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var info = new DirectoryInfo(candidate);
            if (!info.Exists || info.LinkTarget is not null
                || (info.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                return false;
            }

            return !HasReparsePointInChain(candidate, authorizedDirectory);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException
                or NotSupportedException or System.Security.SecurityException)
        {
            return false;
        }
    }

    private static string? SelectSubsDirectory(List<string> subs)
    {
        string? fallback = null;
        foreach (var directory in subs)
        {
            var name = Path.GetFileName(directory);
            if (string.Equals(name, SubsDirectoryName, StringComparison.Ordinal))
            {
                return directory;
            }

            if (fallback is null
                || string.Compare(name, Path.GetFileName(fallback), StringComparison.Ordinal) < 0)
            {
                fallback = directory;
            }
        }

        return fallback;
    }

    private static List<string> ReadSubsFiles(string? selected)
    {
        var files = new List<string>();
        if (selected is null)
        {
            return files;
        }

        string[] entries;
        try
        {
            entries = Directory.GetFileSystemEntries(selected, "*", SearchOption.TopDirectoryOnly);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException
                or NotSupportedException or System.Security.SecurityException)
        {
            return files;
        }

        var prefix = string.Concat(Path.GetFileName(selected), "/");
        foreach (var candidate in entries)
        {
            if (IsRegularFileEntry(candidate))
            {
                files.Add(string.Concat(prefix, Path.GetFileName(candidate)));
            }
        }

        return files;
    }

    private static bool IsRegularFileEntry(string candidate)
    {
        try
        {
            if (HasReparsePointInChain(candidate, Path.GetDirectoryName(candidate) ?? candidate))
            {
                return false;
            }

            var info = new FileInfo(candidate);
            if (!info.Exists || info.LinkTarget is not null
                || (info.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            {
                return false;
            }

            return IsRegularFile(candidate);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException
                or NotSupportedException or System.Security.SecurityException)
        {
            return false;
        }
    }

    private string AuthorizeExisting(string candidate)
    {
        var root = ResolveRoot();
        var authorized = ResolveContained(root, candidate);
        if (HasReparsePointInChain(authorized, root))
        {
            throw Denied();
        }

        if (!Directory.Exists(authorized) && !File.Exists(authorized))
        {
            throw new NotFoundException();
        }

        return authorized;
    }

    private static void VerifyOpenedStream(FileStream stream)
    {
        var handle = stream.SafeFileHandle;
        if (handle.IsInvalid)
        {
            throw Denied();
        }

        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(handle);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw Denied();
        }

        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
        {
            throw Denied();
        }
    }

    private static void RejectNonRegularFile(string authorizedPath)
    {
        var info = new FileInfo(authorizedPath);
        if (info.LinkTarget is not null || (info.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
        {
            throw Denied();
        }

        if (!IsRegularFile(authorizedPath))
        {
            throw Denied();
        }
    }

    private static bool IsRegularFile(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return true;
        }

        if (OperatingSystem.IsLinux())
        {
            return IsRegularFileStatX(path);
        }

        if (OperatingSystem.IsMacOS())
        {
            return IsRegularFileLstat(path);
        }

        return true;
    }

    private static bool IsRegularFileStatX(string path)
    {
        var buffer = new byte[StatXBufferSize];
        if (StatX(AtFdcwd, path, AtSymlinkNofollow, StatXType, buffer) != 0)
        {
            return true;
        }

        return (BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(StatXModeOffset, sizeof(ushort))) & FileTypeMask) == RegularFileBits;
    }

    private static bool IsRegularFileLstat(string path)
    {
        var buffer = new byte[LstatBufferSize];
        if (LstatMacOS(path, buffer) != 0)
        {
            return true;
        }

        return (BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(LstatModeOffset, sizeof(ushort))) & FileTypeMask) == RegularFileBits;
    }

    private const int AtFdcwd = -100;
    private const int AtSymlinkNofollow = 0x100;
    private const uint StatXType = 0x0001;
    private const int StatXBufferSize = 256;
    private const int StatXModeOffset = 28;
    private const int LstatBufferSize = 512;
    private const int LstatModeOffset = 4;
    private const ushort FileTypeMask = 0xF000;
    private const ushort RegularFileBits = 0x8000;

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8, EntryPoint = "statx")]
    private static partial int StatX(int dirfd, string pathname, int flags, uint mask, [Out] byte[] buf);

    [LibraryImport("libSystem.dylib", SetLastError = true, StringMarshalling = StringMarshalling.Utf8, EntryPoint = "lstat$INODE64")]
    private static partial int LstatMacOS(string pathname, [Out] byte[] buf);

    private static FileStream OpenAuthorized(string authorizedPath) =>
        new(
            authorizedPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            StreamBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

    private static LibraryFileAccessDeniedException Denied() =>
        new("The requested library path is outside the authorized library root or is not a regular file.");


    private string ResolveRoot()
    {
        var configured = libraryOptions.Value.RootPath;
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw Denied();
        }

        var absolute = Path.IsPathRooted(configured) ? configured : Path.Combine(ContentRoot(), configured);
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(absolute));
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new LibraryFileAccessDeniedException("The configured library root is not a usable path.", exception);
        }
    }

    private string ContentRoot() =>
        string.IsNullOrWhiteSpace(hostEnvironment.ContentRootPath)
            ? AppContext.BaseDirectory
            : hostEnvironment.ContentRootPath;

    private static string ResolveContained(string root, string candidate)
    {
        string candidateFullPath;
        try
        {
            var combined = Path.IsPathRooted(candidate)
                ? candidate
                : Path.Combine(root, candidate.Trim('/', '\\'));
            candidateFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(combined));
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new LibraryFileAccessDeniedException("The requested library path is not a usable path.", exception);
        }

        if (!IsContained(root, candidateFullPath))
        {
            throw Denied();
        }

        return candidateFullPath;
    }

    private static bool IsContained(string root, string candidate)
    {
        if (string.Equals(root, candidate, PathComparison))
        {
            return true;
        }

        var boundary = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(boundary, PathComparison);
    }

    private static bool HasReparsePointInChain(string candidate, string root)
    {
        var current = candidate;
        while (!string.IsNullOrEmpty(current))
        {
            if (string.Equals(current, root, PathComparison))
            {
                return false;
            }

            if (IsReparsePoint(current))
            {
                return true;
            }

            var parent = Path.GetDirectoryName(current);
            if (parent is null || string.Equals(parent, current, PathComparison))
            {
                return false;
            }

            current = parent;
        }

        return false;
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            if (new FileInfo(path).LinkTarget is not null)
            {
                return true;
            }

            if (new DirectoryInfo(path).LinkTarget is not null)
            {
                return true;
            }

            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or DirectoryNotFoundException or ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
