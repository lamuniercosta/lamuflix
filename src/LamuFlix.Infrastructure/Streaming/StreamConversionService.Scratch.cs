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
        List<CompletedAsset> expired;
        lock (gate)
        {
            var now = timeProvider.GetUtcNow();
            expired =
            [
                .. assets.Values.Where(asset => asset.Readers == 0
                    && (now - asset.LastAccess >= options.IdleExpiry || !IsCurrentVersion(asset.Version))),
            ];
            foreach (var asset in expired)
            {
                assets.Remove(asset.Version);
            }
        }

        foreach (var asset in expired)
        {
            DeleteQuietly(asset.Path);
        }
    }

    private void EnforceSizeLimit()
    {
        while (GetTotalAssetLength() > options.ScratchSizeLimitBytes)
        {
            CompletedAsset? victim;
            lock (gate)
            {
                victim = assets.Values
                    .Where(asset => asset.Readers == 0)
                    .OrderBy(asset => asset.LastAccess)
                    .FirstOrDefault();
                if (victim is null)
                {
                    return;
                }

                assets.Remove(victim.Version);
            }

            DeleteQuietly(victim.Path);
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
        List<CompletedAsset> stale;
        lock (gate)
        {
            stale = [.. assets.Values.Where(asset => asset.Version.Path == sourcePath && asset.Version != version)];
            foreach (var asset in stale.Where(asset => asset.Readers == 0))
            {
                assets.Remove(asset.Version);
            }
        }

        foreach (var asset in stale.Where(asset => asset.Readers == 0))
        {
            DeleteQuietly(asset.Path);
        }
    }

    private void Invalidate(SourceVersion version)
    {
        CompletedAsset? asset;
        lock (gate)
        {
            probes.Remove(version);
            inFlight.Remove(version);
            asset = assets.GetValueOrDefault(version);
            if (asset is { Readers: 0 })
            {
                assets.Remove(version);
            }
            else
            {
                asset = null;
            }
        }

        if (asset is not null)
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
