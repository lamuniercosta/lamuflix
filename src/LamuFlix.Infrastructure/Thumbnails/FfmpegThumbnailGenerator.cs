using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Thumbnails;

public sealed class FfmpegThumbnailGenerator : IThumbnailGenerator
{
    private const string FfprobeExecutable = "ffprobe";

    private readonly IProcessRunner runner;
    private readonly ThumbnailOptions options;
    private readonly IHostEnvironment hostEnvironment;

    public FfmpegThumbnailGenerator(
        IOptions<ThumbnailOptions> options,
        IProcessRunner runner,
        IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
        this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
        this.hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
    }

    public async Task<ThumbnailResult> GenerateAsync(
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return Failed("Disabled");
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return Failed("InvalidSourcePath");
        }

        var timeout = TimeSpan.FromSeconds(options.ProcessTimeoutSeconds);
        var probe = await runner.RunAsync(
            ResolveFfprobePath(options.FfmpegPath),
            ProbeArguments(sourcePath),
            timeout,
            cancellationToken).ConfigureAwait(false);

        if (!probe.Success)
        {
            return Failed(MapProbeFailure(probe.FailureReason));
        }

        if (!TryReadDuration(probe.StandardOutput, out var duration, out var probeFailure))
        {
            return Failed(probeFailure);
        }

        var cacheRoot = ResolveCacheRoot(options);
        try
        {
            Directory.CreateDirectory(cacheRoot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failed("StorageError");
        }

        var outputPath = Path.Combine(cacheRoot, Guid.NewGuid().ToString("N") + ".jpg");
        try
        {
            var extraction = await runner.RunAsync(
                options.FfmpegPath,
                ExtractionArguments(sourcePath, duration, outputPath),
                timeout,
                cancellationToken).ConfigureAwait(false);

            if (!extraction.Success)
            {
                return Failed(MapExtractionFailure(extraction.FailureReason));
            }

            return await ValidateOutputAsync(outputPath, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            DeleteQuietly(outputPath);
            throw;
        }
    }

    private static IReadOnlyList<string> ProbeArguments(string sourcePath) =>
    [
        "-v",
        "error",
        "-select_streams",
        "v:0",
        "-show_entries",
        "stream=codec_type,duration:format=duration",
        "-of",
        "json",
        sourcePath,
    ];

    private IReadOnlyList<string> ExtractionArguments(
        string sourcePath,
        double duration,
        string outputPath)
    {
        var seek = (duration * 0.10).ToString(CultureInfo.InvariantCulture);
        return
        [
            "-nostdin",
            "-y",
            "-ss",
            seek,
            "-i",
            sourcePath,
            "-map",
            "0:v:0",
            "-frames:v",
            "1",
            "-vf",
            $"scale=min({options.MaxWidth.ToString(CultureInfo.InvariantCulture)}\\,iw):-2",
            outputPath,
        ];
    }

    private static bool TryReadDuration(
        string standardOutput,
        out double duration,
        out string failureReason)
    {
        duration = 0;
        failureReason = "ProbeFailed";

        try
        {
            using var document = JsonDocument.Parse(standardOutput);
            if (!TryExtractDurationText(document.RootElement, out var durationText))
            {
                failureReason = "NoVideoStream";
                return false;
            }

            if (!TryParsePositiveFinite(durationText, out duration))
            {
                failureReason = "InvalidDuration";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
        catch (JsonException)
        {
            failureReason = "ProbeFailed";
            return false;
        }
    }

    private static bool TryExtractDurationText(JsonElement root, out string? durationText)
    {
        durationText = null;
        if (!TryGetVideoStreams(root, out var streams))
        {
            return false;
        }

        if (!TryReadStreamDuration(streams, out var streamDuration))
        {
            return false;
        }

        durationText = string.IsNullOrWhiteSpace(streamDuration)
            ? TryGetFormatDuration(root)
            : streamDuration;
        return true;
    }

    private static bool TryGetVideoStreams(JsonElement root, out JsonElement streams)
    {
        streams = default;
        if (!root.TryGetProperty("streams", out var value)
            || value.ValueKind != JsonValueKind.Array
            || value.GetArrayLength() == 0)
        {
            return false;
        }

        streams = value;
        return true;
    }

    private static string? TryGetFormatDuration(JsonElement root)
    {
        if (root.TryGetProperty("format", out var format)
            && format.TryGetProperty("duration", out var formatDuration)
            && formatDuration.ValueKind == JsonValueKind.String)
        {
            return formatDuration.GetString();
        }

        return null;
    }

    private static bool TryParsePositiveFinite(string? durationText, out double duration)
    {
        duration = 0;
        return double.TryParse(
                durationText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out duration)
            && double.IsFinite(duration)
            && duration > 0;
    }

    private static bool TryReadStreamDuration(JsonElement streams, out string? duration)
    {
        duration = null;
        foreach (var stream in streams.EnumerateArray())
        {
            if (!IsVideoStream(stream))
            {
                continue;
            }

            duration = TryGetStreamDuration(stream);
            return true;
        }

        return false;
    }

    private static bool IsVideoStream(JsonElement stream)
    {
        return stream.TryGetProperty("codec_type", out var codecType)
            && codecType.ValueKind == JsonValueKind.String
            && codecType.GetString() == "video";
    }

    private static string? TryGetStreamDuration(JsonElement stream)
    {
        if (stream.TryGetProperty("duration", out var streamDuration)
            && streamDuration.ValueKind == JsonValueKind.String)
        {
            return streamDuration.GetString();
        }

        return null;
    }

    private static async Task<ThumbnailResult> ValidateOutputAsync(
        string outputPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var information = new FileInfo(outputPath);
            if (!information.Exists || information.Length < 2)
            {
                DeleteQuietly(outputPath);
                return Failed("InvalidOutput");
            }

            bool valid;
            await using (var stream = new FileStream(
                outputPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            {
                var header = new byte[2];
                var read = await stream.ReadAsync(header, cancellationToken).ConfigureAwait(false);
                valid = read == header.Length && header[0] == 0xFF && header[1] == 0xD8;
            }

            if (!valid)
            {
                DeleteQuietly(outputPath);
                return Failed("InvalidOutput");
            }

            return new ThumbnailResult(true, outputPath, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            DeleteQuietly(outputPath);
            return Failed("StorageError");
        }
    }

    private string ResolveCacheRoot(ThumbnailOptions thumbnailOptions)
    {
        if (Path.IsPathRooted(thumbnailOptions.CacheDirectory))
        {
            return thumbnailOptions.CacheDirectory;
        }

        var contentRoot = string.IsNullOrWhiteSpace(hostEnvironment.ContentRootPath)
            ? AppContext.BaseDirectory
            : hostEnvironment.ContentRootPath;
        return Path.Combine(contentRoot, thumbnailOptions.CacheDirectory);
    }

    private static string ResolveFfprobePath(string ffmpegPath)
    {
        var directory = Path.GetDirectoryName(ffmpegPath);
        if (string.IsNullOrEmpty(directory))
        {
            return FfprobeExecutable;
        }

        return Path.Combine(directory, FfprobeExecutable + Path.GetExtension(ffmpegPath));
    }

    private static string MapProbeFailure(string? failureReason) =>
        failureReason switch
        {
            "LaunchFailed" => "BinaryNotFound",
            "TimedOut" => "ProcessTimedOut",
            _ => "ProbeFailed",
        };

    private static string MapExtractionFailure(string? failureReason) =>
        failureReason switch
        {
            "LaunchFailed" => "BinaryNotFound",
            "TimedOut" => "ProcessTimedOut",
            _ => "ExtractionFailed",
        };

    private static void DeleteQuietly(string outputPath)
    {
        try
        {
            File.Delete(outputPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup; a failed delete must not mask the original failure result.
        }
    }

    private static ThumbnailResult Failed(string failureReason) =>
        new(false, null, failureReason);
}
