using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;

namespace LamuFlix.Infrastructure.Streaming;

public sealed partial class StreamConversionService
{
    public Task<StreamReaderLease> OpenReadAsync(
        StreamConversionResult prepared,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        cancellationToken.ThrowIfCancellationRequested();

        Action? release = null;
        lock (gate)
        {
            var asset = assets.Values.FirstOrDefault(candidate =>
                string.Equals(candidate.Path, prepared.Path, StringComparison.Ordinal));
            if (asset is not null)
            {
                asset.Readers++;
                asset.LastAccess = timeProvider.GetUtcNow();
                release = () => ReleaseReader(asset);
            }
        }

        FileStream stream;
        try
        {
            stream = new FileStream(
                prepared.Path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                4096,
                FileOptions.SequentialScan);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException)
        {
            release?.Invoke();
            throw;
        }

        return Task.FromResult(new StreamReaderLease(stream, release));
    }

    public Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RemoveExpiredAssets();
        EnforceSizeLimit();
        DeleteOwnedOrphans();
        return Task.CompletedTask;
    }

    private void ReleaseReader(CompletedAsset asset)
    {
        lock (gate)
        {
            asset.Readers--;
            asset.LastAccess = timeProvider.GetUtcNow();
        }
    }

    private void RemoveExpiredAssets()
    {
        var now = timeProvider.GetUtcNow();
        var expired = TakeAssets(asset => asset.Readers == 0
            && (now - asset.LastAccess >= options.IdleExpiry || !IsCurrentVersion(asset.Version)));

        foreach (var asset in expired)
        {
            DeleteQuietly(asset.Path);
        }
    }

    private void EnforceSizeLimit()
    {
        while (GetTotalAssetLength() > options.ScratchSizeLimitBytes)
        {
            var victim = TakeOldestIdle();
            if (victim is null)
            {
                return;
            }

            DeleteQuietly(victim.Path);
        }
    }

    private List<CompletedAsset> TakeAssets(Func<CompletedAsset, bool> predicate)
    {
        lock (gate)
        {
            var taken = assets.Values.Where(predicate).ToList();
            foreach (var asset in taken)
            {
                assets.Remove(asset.Version);
            }

            return taken;
        }
    }

    private CompletedAsset? TakeOldestIdle()
    {
        lock (gate)
        {
            var victim = assets.Values
                .Where(static asset => asset.Readers == 0)
                .OrderBy(static asset => asset.LastAccess)
                .FirstOrDefault();
            if (victim is not null)
            {
                assets.Remove(victim.Version);
            }

            return victim;
        }
    }

    private long GetTotalAssetLength()
    {
        lock (gate)
        {
            return assets.Values.Sum(asset => asset.Length);
        }
    }

    private void RemoveStaleAssets(string sourcePath, SourceVersion version)
    {
        var stale = TakeAssets(asset => asset.Version.Path == sourcePath
            && asset.Version != version
            && asset.Readers == 0);

        foreach (var asset in stale)
        {
            DeleteQuietly(asset.Path);
        }
    }

    private void Invalidate(SourceVersion version)
    {
        lock (gate)
        {
            probes.Remove(version);
            inFlight.Remove(version);
        }

        var removed = TakeAssets(asset => asset.Version == version && asset.Readers == 0);
        foreach (var asset in removed)
        {
            DeleteQuietly(asset.Path);
        }
    }

    private static bool IsCurrentVersion(SourceVersion version)
    {
        try
        {
            return GetVersion(version.Path) == version;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or FileNotFoundException)
        {
            return false;
        }
    }

    private void DeleteOwnedOrphans()
    {
        if (!Directory.Exists(scratchRoot))
        {
            return;
        }

        HashSet<string> active;
        lock (gate)
        {
            active = inFlight.Values
                .Select(flight => Path.GetFileName(flight.PartialPath))
                .ToHashSet(StringComparer.Ordinal);
        }

        foreach (var file in Directory.EnumerateFiles(scratchRoot))
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith(OwnedPrefix, StringComparison.Ordinal)
                && (name.EndsWith(".partial.mp4", StringComparison.Ordinal) || name.EndsWith(".tmp", StringComparison.Ordinal))
                && !active.Contains(name))
            {
                DeleteQuietly(file);
            }
        }
    }

    private static IReadOnlyList<string> ConversionArguments(
        string sourcePath,
        int audioOrdinal,
        string outputPath) =>
    [
        "-nostdin",
        "-y",
        "-i",
        sourcePath,
        "-map",
        "0:v:0",
        "-map",
        "0:a:" + audioOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture),
        "-c:v",
        "copy",
        "-c:a",
        "aac",
        "-b:a",
        "192k",
        "-ac",
        "2",
        outputPath,
    ];

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
