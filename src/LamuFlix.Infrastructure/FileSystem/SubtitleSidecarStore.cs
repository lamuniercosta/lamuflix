using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.FileSystem;

public sealed partial class SubtitleSidecarStore(
    ILibraryFileReader reader,
    IOptions<LibraryOptions> libraryOptions,
    IHostEnvironment hostEnvironment) : ISubtitleSidecarStore
{
    private const string SubtitleExtension = ".srt";
    private const string SubsFolderName = "Subs";
    private const int ReadBufferSize = 81920;
    private const int TempFileAttempts = 8;
    private const string SizeKey = "Subtitle";
    private const string SizeMessage = "The subtitle sidecar exceeds the configured serving bound.";
    private const int ErrnoExist = 17; // EEXIST on Linux and macOS.

    public Task<bool> ExistsAsync(
        LibraryPath folder,
        string primaryFileName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(folder);
        cancellationToken.ThrowIfCancellationRequested();

        var fileName = SubtitleFileName(primaryFileName);
        foreach (var candidate in reader.EnumerateFiles(folder))
        {
            if (string.Equals(candidate, fileName, NameComparison))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public async Task<byte[]> ReadAsync(
        LibraryPath folder,
        string primaryFileName,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);

        var sidecar = SidecarPath(folder, SubtitleFileName(primaryFileName));
        await using var stream = reader.OpenRead(sidecar);
        return await ReadBoundedAsync(stream, maxBytes, cancellationToken);
    }

    public async Task<SubtitleSidecarWriteResult> CreateAsync(
        LibraryPath folder,
        string primaryFileName,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var fileName = SubtitleFileName(primaryFileName);
        var directory = ResolveWritableDirectory(folder);
        AuthorizePrimary(folder, primaryFileName);
        var target = Path.Combine(directory, fileName);

        if (TargetExists(target))
        {
            return SubtitleSidecarWriteResult.AlreadyPresent;
        }

        var temp = TempPath(directory, fileName);
        try
        {
            await WriteTempAsync(temp, content, cancellationToken);
            return Commit(temp, target);
        }
        catch (OperationCanceledException)
        {
            DeleteQuietly(temp);
            throw;
        }
        finally
        {
            DeleteQuietly(temp);
        }
    }

    public Task EnsurePublishableAsync(
        LibraryPath folder,
        string primaryFileName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(folder);
        cancellationToken.ThrowIfCancellationRequested();

        _ = SubtitleStem(primaryFileName);
        AuthorizePrimary(folder, primaryFileName);

        var movieDirectory = ResolveExistingMovieDirectory(folder);
        EnforceSubsContainment(movieDirectory, Path.Combine(movieDirectory, SubsFolderName));
        return Task.CompletedTask;
    }

    public async Task<SubtitlePublishResult> PublishToSubsAsync(
        LibraryPath folder,
        string primaryFileName,
        string languageCode,
        ReadOnlyMemory<byte> content,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);

        var stem = SubtitleStem(primaryFileName);
        var baseName = string.Concat(stem, "-", CanonicalLanguageCode(languageCode));
        AuthorizePrimary(folder, primaryFileName);

        var movieDirectory = ResolveExistingMovieDirectory(folder);
        var subsDirectory = Path.Combine(movieDirectory, SubsFolderName);
        EnforceSubsContainment(movieDirectory, subsDirectory);

        if (!Directory.Exists(subsDirectory))
        {
            Directory.CreateDirectory(subsDirectory);
        }

        EnforceSubsContainment(movieDirectory, subsDirectory);
        if (!Directory.Exists(subsDirectory))
        {
            throw Denied();
        }

        var staged = StagePath(subsDirectory, baseName);
        try
        {
            await WriteTempAsync(staged, content, cancellationToken).ConfigureAwait(false);
            return PublishStaged(staged, subsDirectory, movieDirectory, baseName, maxAttempts, cancellationToken);
        }
        finally
        {
            DeleteQuietly(staged);
        }
    }

    private static SubtitlePublishResult PublishStaged(
        string staged,
        string subsDirectory,
        string movieDirectory,
        string baseName,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnforceSubsContainment(movieDirectory, subsDirectory);

            var fileName = CollisionFileName(baseName, attempt);
            if (Commit(staged, Path.Combine(subsDirectory, fileName)) == SubtitleSidecarWriteResult.Created)
            {
                return SubtitlePublishResult.Published(fileName);
            }
        }

        return SubtitlePublishResult.AttemptsExhausted();
    }

    private static string CollisionFileName(string baseName, int attempt) =>
        attempt == 1
            ? string.Concat(baseName, SubtitleExtension)
            : string.Concat(
                baseName,
                "(",
                (attempt - 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ")",
                SubtitleExtension);

    private static string StagePath(string subsDirectory, string baseName)
    {
        var candidate = Path.Combine(subsDirectory, string.Concat(".", baseName, ".", Guid.NewGuid().ToString("N"), SubtitleExtension));
        return candidate;
    }

    private string ResolveExistingMovieDirectory(LibraryPath folder)
    {
        var root = ResolveRoot();
        var combined = LibraryPath.IsAbsolute(folder.Value)
            ? folder.Value
            : Path.Combine(root, folder.Value.Trim('/', '\\'));

        string directory;
        try
        {
            directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(combined));
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw Denied();
        }

        if (!IsContained(root, directory) || HasReparsePointInChain(directory, root))
        {
            throw Denied();
        }

        if (!Directory.Exists(directory))
        {
            throw new NotFoundException();
        }

        return directory;
    }

    private static void EnforceSubsContainment(string movieDirectory, string subsDirectory)
    {
        var expected = Path.TrimEndingDirectorySeparator(Path.Combine(movieDirectory, SubsFolderName));
        if (!string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(subsDirectory)),
            Path.GetFullPath(expected),
            PathComparison))
        {
            throw Denied();
        }

        if (!IsContained(movieDirectory, subsDirectory) || HasReparsePointInChain(subsDirectory, movieDirectory))
        {
            throw Denied();
        }

        if (IsReparsePoint(subsDirectory))
        {
            throw Denied();
        }
    }

    private static string SubtitleStem(string primaryFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryFileName);

        if (primaryFileName.IndexOfAny(['/', '\\']) >= 0)
        {
            throw Denied();
        }

        var stem = Path.GetFileNameWithoutExtension(primaryFileName);
        if (string.IsNullOrEmpty(stem) || stem is "." or ".."
            || stem.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw Denied();
        }

        return stem;
    }

    private static string CanonicalLanguageCode(string languageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        var trimmed = languageCode.Trim();
        if (trimmed is "." or ".."
            || trimmed.IndexOfAny(['/', '\\', '.']) >= 0
            || trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || !trimmed.All(static character => char.IsAsciiLetterOrDigit(character) || character == '-'))
        {
            throw Denied();
        }

        return trimmed;
    }

    private static SubtitleSidecarWriteResult Commit(string temp, string target) =>
        OperatingSystem.IsWindows() ? CommitByMove(temp, target) : CommitByLink(temp, target);

    private static SubtitleSidecarWriteResult CommitByMove(string temp, string target)
    {
        try
        {
            File.Move(temp, target, overwrite: false);
        }
        catch (IOException) when (TargetExists(target))
        {
            return SubtitleSidecarWriteResult.AlreadyPresent;
        }

        return SubtitleSidecarWriteResult.Created;
    }

    // File.Move(overwrite: false) is not atomic on Unix: it checks for the target and then calls
    // rename(2), which silently replaces a file that a racing writer created in between. link(2)
    // fails with EEXIST atomically instead, and the complete temp file becomes the target in one step.
    private static SubtitleSidecarWriteResult CommitByLink(string temp, string target)
    {
        if (OperatingSystem.IsMacOS() ? LinkMacOS(temp, target) == 0 : LinkLinux(temp, target) == 0)
        {
            return SubtitleSidecarWriteResult.Created;
        }

        if (Marshal.GetLastPInvokeError() == ErrnoExist)
        {
            return SubtitleSidecarWriteResult.AlreadyPresent;
        }

        throw new IOException("The subtitle sidecar could not be created without replacing an existing file.");
    }

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8, EntryPoint = "link")]
    private static partial int LinkLinux(string existingPath, string newPath);

    [LibraryImport("libSystem.dylib", SetLastError = true, StringMarshalling = StringMarshalling.Utf8, EntryPoint = "link")]
    private static partial int LinkMacOS(string existingPath, string newPath);

    private static async Task WriteTempAsync(
        string temp,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        var stream = new FileStream(
            temp,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            ReadBufferSize,
            FileOptions.Asynchronous);
        await using (stream.ConfigureAwait(false))
        {
            await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<byte[]> ReadBoundedAsync(
        Stream stream,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
        try
        {
            using var content = new MemoryStream();
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, ReadBufferSize), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                if (content.Length + read > maxBytes)
                {
                    throw new ValidationException(
                        new Dictionary<string, string[]>(StringComparer.Ordinal) { [SizeKey] = [SizeMessage] });
                }

                content.Write(buffer, 0, read);
            }

            return content.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void AuthorizePrimary(LibraryPath folder, string primaryFileName)
    {
        var combined = LibraryPath.IsAbsolute(folder.Value)
            ? Path.Combine(folder.Value, primaryFileName)
            : string.Concat(folder.Value.Trim('/'), "/", primaryFileName);

        if (!LibraryPath.TryCreate(combined, out var primary))
        {
            throw Denied();
        }

        using var stream = reader.OpenRead(primary);
    }

    private string ResolveWritableDirectory(LibraryPath folder)
    {
        reader.AuthorizeFolder(folder);

        var root = ResolveRoot();
        var combined = LibraryPath.IsAbsolute(folder.Value)
            ? folder.Value
            : Path.Combine(root, folder.Value.Trim('/', '\\'));
        var directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(combined));

        if (!IsContained(root, directory) || HasReparsePointInChain(directory, root))
        {
            throw Denied();
        }

        if (!Directory.Exists(directory))
        {
            throw new NotFoundException();
        }

        return directory;
    }

    private static LibraryPath SidecarPath(LibraryPath folder, string fileName)
    {
        var combined = LibraryPath.IsAbsolute(folder.Value)
            ? Path.Combine(folder.Value, fileName)
            : string.Concat(folder.Value.Trim('/'), "/", fileName);

        return LibraryPath.TryCreate(combined, out var path) ? path : throw Denied();
    }

    private static string SubtitleFileName(string primaryFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryFileName);

        if (!string.Equals(Path.GetFileName(primaryFileName), primaryFileName, StringComparison.Ordinal))
        {
            throw Denied();
        }

        var stem = Path.GetFileNameWithoutExtension(primaryFileName);
        if (string.IsNullOrEmpty(stem) || stem is "." or ".."
            || stem.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw Denied();
        }

        return string.Concat(stem, SubtitleExtension);
    }

    private static bool TargetExists(string target)
    {
        var info = new FileInfo(target);
        if (info.LinkTarget is not null)
        {
            throw Denied();
        }

        if (Directory.Exists(target))
        {
            throw Denied();
        }

        return info.Exists;
    }

    private static string TempPath(string directory, string fileName)
    {
        for (var attempt = 0; attempt < TempFileAttempts; attempt++)
        {
            var candidate = Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw Denied();
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // Cancellation cleanup is best effort (accepted personal-scale crash-orphan risk).
        }
    }

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
        while (!string.IsNullOrEmpty(current) && !IsSamePath(current, root))
        {
            if (IsReparsePoint(current))
            {
                return true;
            }

            current = ParentOrEmpty(current);
        }

        return false;
    }

    private static string ParentOrEmpty(string current)
    {
        var parent = Path.GetDirectoryName(current);
        return parent is null || IsSamePath(parent, current) ? string.Empty : parent;
    }

    private static bool IsSamePath(string left, string right) =>
        string.Equals(left, right, PathComparison);

    private static bool IsReparsePoint(string path)
    {
        try
        {
            if (new FileInfo(path).LinkTarget is not null || new DirectoryInfo(path).LinkTarget is not null)
            {
                return true;
            }

            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or DirectoryNotFoundException or ArgumentException
                or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static LibraryFileAccessDeniedException Denied() =>
        new("The requested library path is outside the authorized library root or is not a regular file.");

    private static StringComparison NameComparison => OrdinalFileComparison;

    private static StringComparison PathComparison => OrdinalFileComparison;

    private static StringComparison OrdinalFileComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
