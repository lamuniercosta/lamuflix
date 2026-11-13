using System;
using System.Globalization;
using System.IO;
using System.Threading;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Thumbnails;

public sealed class ThumbnailCache
{
    private const byte JpegSoiFirstByte = 0xFF;
    private const byte JpegSoiSecondByte = 0xD8;
    private const int JpegSoiLength = 2;
    private const string CanonicalExtension = ".jpg";
    private const int TemporaryNameLength = 32;
    private const int StreamBufferSize = 4096;
    private const int OpenAttempts = 3;
    private static readonly TimeSpan OpenRetryDelay = TimeSpan.FromMilliseconds(10);

    private readonly string cacheRoot;

    public ThumbnailCache(IOptions<ThumbnailOptions> options, IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        cacheRoot = ResolveCacheRoot(options.Value, hostEnvironment);
    }

    public bool TryOpenCanonical(MovieId movieId, out FileStream? stream)
    {
        ArgumentNullException.ThrowIfNull(movieId);

        stream = null;
        if (!TryResolveCanonicalPath(movieId, out var canonicalPath))
        {
            return false;
        }

        for (var attempt = 1; ; attempt++)
        {
            FileStream? candidate = null;
            try
            {
                candidate = OpenSharedRead(canonicalPath);
                if (!HasJpegSoi(candidate))
                {
                    candidate.Dispose();
                    return false;
                }

                candidate.Position = 0;
                stream = candidate;
                return true;
            }
            catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
            {
                candidate?.Dispose();
                return false;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                candidate?.Dispose();
                if (attempt >= OpenAttempts)
                {
                    return false;
                }

                Thread.Sleep(OpenRetryDelay);
            }
        }
    }

    public bool TryPromote(string generatedPath, MovieId movieId)
    {
        ArgumentNullException.ThrowIfNull(movieId);

        if (string.IsNullOrWhiteSpace(generatedPath) || !IsTemporaryFileName(generatedPath))
        {
            return false;
        }

        try
        {
            if (!IsPromotable(generatedPath, movieId, out var generatedFullPath, out var canonicalPath))
            {
                return false;
            }

            ReplaceAtomically(generatedFullPath, canonicalPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            DeleteTemporary(generatedPath);
        }
    }

    private bool IsPromotable(string generatedPath, MovieId movieId, out string generatedFullPath, out string canonicalPath)
    {
        generatedFullPath = string.Empty;
        canonicalPath = string.Empty;
        return TryResolveCanonicalPath(movieId, out canonicalPath)
            && TryResolveContainedPath(generatedPath, out generatedFullPath)
            && !IsSamePath(generatedFullPath, canonicalPath)
            && HasValidJpeg(generatedFullPath);
    }

    public void DeleteTemporary(string generatedPath)
    {
        if (string.IsNullOrWhiteSpace(generatedPath) || !IsTemporaryFileName(generatedPath))
        {
            return;
        }

        if (!TryResolveContainedPath(generatedPath, out var generatedFullPath))
        {
            return;
        }

        try
        {
            File.Delete(generatedFullPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup; a failed delete must not mask the original outcome.
        }
    }

    private static string ResolveCacheRoot(ThumbnailOptions options, IHostEnvironment hostEnvironment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.CacheDirectory);

        var configured = options.CacheDirectory;
        if (!Path.IsPathRooted(configured))
        {
            var contentRoot = string.IsNullOrWhiteSpace(hostEnvironment.ContentRootPath)
                ? AppContext.BaseDirectory
                : hostEnvironment.ContentRootPath;
            configured = Path.Combine(contentRoot, configured);
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(configured));
    }

    private static FileStream OpenSharedRead(string path) =>
        new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            StreamBufferSize,
            FileOptions.Asynchronous);

    private static bool HasJpegSoi(FileStream stream)
    {
        Span<byte> header = stackalloc byte[JpegSoiLength];
        var read = stream.Read(header);
        return read == JpegSoiLength
            && header[0] == JpegSoiFirstByte
            && header[1] == JpegSoiSecondByte;
    }

    private static bool HasValidJpeg(string path)
    {
        var information = new FileInfo(path);
        if (!information.Exists || information.Length < JpegSoiLength)
        {
            return false;
        }

        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            StreamBufferSize);
        return HasJpegSoi(stream);
    }

    private static void ReplaceAtomically(string source, string destination)
    {
        if (!File.Exists(destination))
        {
            File.Move(source, destination, overwrite: true);
            return;
        }

        try
        {
            File.Move(source, destination, overwrite: true);
        }
        catch (FileNotFoundException)
        {
            File.Move(source, destination, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            File.Replace(source, destination, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }
    }

    private static bool IsTemporaryFileName(string path)
    {
        var name = Path.GetFileName(path);
        if (name.Length != TemporaryNameLength + CanonicalExtension.Length
            || !name.EndsWith(CanonicalExtension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Guid.TryParseExact(Path.GetFileNameWithoutExtension(name), "N", out _);
    }

    private bool TryResolveCanonicalPath(MovieId movieId, out string canonicalPath)
    {
        var fileName = movieId.Value.ToString(CultureInfo.InvariantCulture) + CanonicalExtension;
        return TryResolveContainedPath(Path.Combine(cacheRoot, fileName), out canonicalPath);
    }

    private bool TryResolveContainedPath(string candidate, out string fullPath)
    {
        fullPath = string.Empty;

        string candidateFullPath;
        try
        {
            candidateFullPath = Path.GetFullPath(candidate);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        if (!IsUnderCacheRoot(candidateFullPath) || HasReparsePointInChain(candidateFullPath))
        {
            return false;
        }

        fullPath = candidateFullPath;
        return true;
    }

    private bool IsUnderCacheRoot(string fullPath)
    {
        var rootWithSeparator = cacheRoot + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(
            rootWithSeparator,
            PathComparison);
    }

    private bool HasReparsePointInChain(string fullPath)
    {
        var current = fullPath;
        while (!string.IsNullOrEmpty(current))
        {
            if (IsReparsePoint(current))
            {
                return true;
            }

            if (IsSamePath(current, cacheRoot))
            {
                return false;
            }

            var parent = Path.GetDirectoryName(current);
            if (parent is null || IsSamePath(parent, current))
            {
                return false;
            }

            current = parent;
        }

        return false;
    }

    private static bool IsSamePath(string left, string right) =>
        string.Equals(left, right, PathComparison);

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
