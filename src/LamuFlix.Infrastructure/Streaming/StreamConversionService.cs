using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Streaming;

public sealed partial class StreamConversionService : IStreamConversionService
{
    private const string OwnedPrefix = "lfstream-";
    private static readonly TimeSpan ReattachmentGrace = TimeSpan.FromSeconds(1);

    private readonly StreamConversionOptions options;
    private readonly ThumbnailOptions thumbnailOptions;
    private readonly IProcessRunner runner;
    private readonly ILogger<StreamConversionService> logger;
    private readonly TimeProvider timeProvider;
    private readonly string scratchRoot;
    private readonly SemaphoreSlim conversionSlots;
    private readonly object gate = new();
    private readonly Dictionary<SourceVersion, ProbeCacheEntry> probes = [];
    private readonly Dictionary<SourceVersion, CompletedAsset> assets = [];
    private readonly Dictionary<SourceVersion, InFlightConversion> inFlight = [];

    public StreamConversionService(
        IOptions<StreamConversionOptions> options,
        IOptions<ThumbnailOptions> thumbnailOptions,
        IProcessRunner runner,
        IHostEnvironment environment,
        ILogger<StreamConversionService> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(thumbnailOptions);
        ArgumentNullException.ThrowIfNull(environment);
        this.options = options.Value;
        this.thumbnailOptions = thumbnailOptions.Value;
        this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        scratchRoot = ResolveScratchRoot(this.options.ScratchDirectory, environment);
        conversionSlots = new SemaphoreSlim(this.options.MaxConcurrentConversions, this.options.MaxConcurrentConversions);
        Directory.CreateDirectory(scratchRoot);
        DeleteOwnedOrphans();
    }

    public async Task<StreamConversionResult> PrepareAsync(
        int movieId,
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(sourcePath);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var version = GetVersion(fullPath);
            RemoveStaleAssets(fullPath, version);
            var selection = await ProbeAsync(movieId, version, fullPath, cancellationToken).ConfigureAwait(false);
            if (!selection.NeedsConversion)
            {
                return new StreamConversionResult(fullPath, false);
            }

            try
            {
                return await ConvertAsync(movieId, version, fullPath, selection.AudioOrdinal, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (SourceVersionChangedException)
            {
                Invalidate(version);
            }
        }

        throw new InvalidOperationException("Source version changed repeatedly.");
    }

    private async Task<StreamConversionResult> ConvertAsync(
        int movieId,
        SourceVersion version,
        string sourcePath,
        int audioOrdinal,
        CancellationToken cancellationToken)
    {
        RemoveExpiredAssets();
        var reused = TryReuseCompleted(version);
        if (reused is not null)
        {
            EnforceSizeLimit(version);
            return reused;
        }

        EnforceSizeLimit();
        var flight = GetOrCreateFlight(movieId, version, sourcePath, audioOrdinal);
        try
        {
            return await flight.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ReleaseWaiter(flight);
        }
    }

    private StreamConversionResult? TryReuseCompleted(SourceVersion version)
    {
        CompletedAsset? candidate;
        lock (gate)
        {
            assets.TryGetValue(version, out candidate);
        }

        if (candidate is null || !IsCurrentVersion(version))
        {
            return null;
        }

        lock (gate)
        {
            if (!assets.TryGetValue(version, out var current) || !ReferenceEquals(current, candidate))
            {
                return null;
            }

            if (current.Readers == 0
                && timeProvider.GetUtcNow() - current.LastAccess >= options.IdleExpiry)
            {
                return null;
            }

            current.LastAccess = timeProvider.GetUtcNow();
            return new StreamConversionResult(current.Path, true);
        }
    }

    private InFlightConversion GetOrCreateFlight(
        int movieId,
        SourceVersion version,
        string sourcePath,
        int audioOrdinal)
    {
        lock (gate)
        {
            if (!inFlight.TryGetValue(version, out var flight))
            {
                var suffix = Guid.NewGuid().ToString("N");
                flight = new InFlightConversion(
                    version,
                    movieId,
                    sourcePath,
                    audioOrdinal,
                    Path.Combine(scratchRoot, OwnedPrefix + suffix + ".partial.mp4"),
                    VersionFileName(version),
                    new CancellationTokenSource(),
                    new TaskCompletionSource<StreamConversionResult>(TaskCreationOptions.RunContinuationsAsynchronously));
                inFlight.Add(version, flight);
                _ = RunConversionAsync(flight);
            }

            flight.Waiters++;
            flight.GraceTimer?.Dispose();
            flight.GraceTimer = null;
            return flight;
        }
    }

    private void ReleaseWaiter(InFlightConversion flight)
    {
        lock (gate)
        {
            flight.Waiters--;
            if (flight.Waiters != 0 || flight.Completion.Task.IsCompleted)
            {
                return;
            }

            flight.GraceTimer = timeProvider.CreateTimer(
                _ => OnGraceExpired(flight),
                null,
                ReattachmentGrace,
                Timeout.InfiniteTimeSpan);
        }
    }

    private void OnGraceExpired(InFlightConversion flight)
    {
        lock (gate)
        {
            if (flight.Waiters != 0
                || flight.Completion.Task.IsCompleted
                || !inFlight.TryGetValue(flight.Version, out var current)
                || !ReferenceEquals(current, flight))
            {
                return;
            }

            inFlight.Remove(flight.Version);
        }

        flight.Completion.TrySetCanceled(flight.Cancellation.Token);
        flight.Cancellation.Cancel();
        DeleteQuietly(flight.PartialPath);
    }

    private async Task RunConversionAsync(InFlightConversion flight)
    {
        try
        {
            await conversionSlots.WaitAsync(flight.Cancellation.Token).ConfigureAwait(false);
            try
            {
                await RunConversionBodyAsync(flight).ConfigureAwait(false);
            }
            finally
            {
                conversionSlots.Release();
            }
        }
        catch (SourceVersionChangedException exception)
        {
            FailFlight(flight, exception, log: false);
        }
        catch (OperationCanceledException)
        {
            DeleteQuietly(flight.PartialPath);
            flight.Completion.TrySetCanceled(flight.Cancellation.Token);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or ArgumentException)
        {
            FailFlight(flight, exception, log: true);
        }
        finally
        {
            lock (gate)
            {
                if (inFlight.TryGetValue(flight.Version, out var current) && ReferenceEquals(current, flight))
                {
                    inFlight.Remove(flight.Version);
                }
            }

            flight.GraceTimer?.Dispose();
        }
    }

    private async Task RunConversionBodyAsync(InFlightConversion flight)
    {
        var current = GetVersion(flight.SourcePath);
        if (current != flight.Version)
        {
            throw new SourceVersionChangedException();
        }

        var result = await runner.RunAsync(
            thumbnailOptions.FfmpegPath,
            ConversionArguments(flight.SourcePath, flight.AudioOrdinal, flight.PartialPath),
            ConversionRunnerTimeout,
            flight.Cancellation.Token).ConfigureAwait(false);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Stream conversion failed for movie {flight.MovieId}: {result.FailureReason}");
        }

        current = GetVersion(flight.SourcePath);
        if (current != flight.Version)
        {
            throw new SourceVersionChangedException();
        }

        var asset = Publish(flight);
        EnforceSizeLimit(asset.Version);
        flight.Completion.TrySetResult(new StreamConversionResult(asset.Path, true));
    }

    private void FailFlight(InFlightConversion flight, Exception exception, bool log)
    {
        if (log)
        {
            logger.LogError(
                exception,
                "Stream conversion failed movieId={MovieId} stage={Stage}",
                flight.MovieId,
                "Conversion");
        }

        DeleteQuietly(flight.PartialPath);
        flight.Completion.TrySetException(exception);
    }

    private CompletedAsset Publish(InFlightConversion flight)
    {
        var finalPath = Path.Combine(scratchRoot, flight.FinalFileName);
        File.Move(flight.PartialPath, finalPath, overwrite: true);
        var asset = new CompletedAsset(
            flight.Version,
            finalPath,
            new FileInfo(finalPath).Length,
            timeProvider.GetUtcNow());
        lock (gate)
        {
            assets[flight.Version] = asset;
        }

        return asset;
    }

    private static TimeSpan ConversionRunnerTimeout => Timeout.InfiniteTimeSpan;

    private static string VersionFileName(SourceVersion version)
    {
        var identity = string.Concat(
            version.Path,
            '\n',
            version.Length.ToString(CultureInfo.InvariantCulture),
            '\n',
            version.LastWriteTicks.ToString(CultureInfo.InvariantCulture));
        return OwnedPrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16] + ".mp4";
    }

    private static SourceVersion GetVersion(string path)
    {
        var information = new FileInfo(path);
        information.Refresh();
        if (!information.Exists)
        {
            throw new FileNotFoundException("Source media not found.", path);
        }

        return new SourceVersion(path, information.Length, information.LastWriteTimeUtc.Ticks);
    }

    private static string ResolveScratchRoot(string configured, IHostEnvironment environment)
    {
        if (Path.IsPathRooted(configured))
        {
            return Path.GetFullPath(configured);
        }

        var contentRoot = string.IsNullOrWhiteSpace(environment.ContentRootPath)
            ? AppContext.BaseDirectory
            : environment.ContentRootPath;
        return Path.GetFullPath(Path.Combine(contentRoot, configured));
    }
}
