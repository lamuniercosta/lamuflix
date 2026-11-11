using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Infrastructure.RabbitMq;

internal static class ThumbnailPathResolver
{
    private const string RootIdentity = ".";
    private const char SegmentSeparator = '/';

    internal static bool TryResolveSource(string moviePathValue, string requestedCanonicalPath, out string sourcePath)
    {
        sourcePath = string.Empty;
        if (string.IsNullOrWhiteSpace(moviePathValue) || string.IsNullOrWhiteSpace(requestedCanonicalPath))
        {
            return false;
        }

        if (!IsSamePath(moviePathValue, requestedCanonicalPath)
            && !IsPrimaryCandidatePath(moviePathValue, requestedCanonicalPath))
        {
            return false;
        }

        sourcePath = requestedCanonicalPath;
        return true;
    }

    internal static bool IsFolderRequest(string moviePathValue, string requestedCanonicalPath) =>
        !string.IsNullOrWhiteSpace(moviePathValue)
        && !string.IsNullOrWhiteSpace(requestedCanonicalPath)
        && IsSamePath(moviePathValue, requestedCanonicalPath);

    internal static string? ResolvePublishPath(LibraryPath moviePath, IMediaLibraryScanner? scanner)
    {
        ArgumentNullException.ThrowIfNull(moviePath);
        if (scanner is null)
        {
            return moviePath.Value;
        }

        try
        {
            return scanner.ResolvePrimaryVideo(moviePath).Path.Value;
        }
        catch (Exception exception) when (exception is NotFoundException or InvalidTransitionException)
        {
            return null;
        }
    }

    private static bool IsSamePath(string first, string second) =>
        string.Equals(
            first.ToUpperInvariant(),
            second.ToUpperInvariant(),
            StringComparison.Ordinal);

    private static bool IsPrimaryCandidatePath(string moviePathValue, string requestedCanonicalPath)
    {
        if (string.Equals(moviePathValue, RootIdentity, StringComparison.Ordinal))
        {
            return !requestedCanonicalPath.Contains(SegmentSeparator);
        }

        var movie = moviePathValue.ToUpperInvariant();
        var requested = requestedCanonicalPath.ToUpperInvariant();
        if (requested.Length <= movie.Length
            || requested[movie.Length] != SegmentSeparator
            || !requested.StartsWith(movie, StringComparison.Ordinal))
        {
            return false;
        }

        return !requested[(movie.Length + 1)..].Contains(SegmentSeparator);
    }
}
