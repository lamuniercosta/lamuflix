using System;
using System.IO;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.FileSystem;

public sealed class LibraryFileReader(
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

        return OpenAuthorized(authorized);
    }

    private string AuthorizeExisting(string candidate)
    {
        var root = ResolveRoot();
        var authorized = ResolveContained(root, candidate);
        if (!Directory.Exists(authorized) && !File.Exists(authorized))
        {
            throw new NotFoundException();
        }

        if (HasReparsePointInChain(authorized, root))
        {
            throw Denied();
        }

        return authorized;
    }

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
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or DirectoryNotFoundException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
