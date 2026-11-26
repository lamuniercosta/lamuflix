using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LamuFlix.Infrastructure.Streaming;

public sealed partial class StreamConversionService
{
    private const int MaxProbeOutputBytes = 64 * 1024;

    private async Task<ProbeSelection> ProbeAsync(
        int movieId,
        SourceVersion version,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        lock (gate)
        {
            if (probes.TryGetValue(version, out var cached))
            {
                return cached.Selection;
            }
        }

        var result = await runner.RunAsync(
            ResolveFfprobePath(),
            ProbeArguments(sourcePath),
            TimeSpan.FromSeconds(thumbnailOptions.ProcessTimeoutSeconds),
            cancellationToken).ConfigureAwait(false);
        if (!result.Success
            || result.StandardOutput.Length > MaxProbeOutputBytes
            || !TryParseProbe(result.StandardOutput, out var selection))
        {
            logger.LogWarning(
                "Stream probe failed bypass movieId={MovieId} reason={ProbeReason} marker={Marker}",
                movieId,
                result.FailureReason ?? "InvalidProbeOutput",
                "probe-failed");
            return new ProbeSelection(-1, string.Empty);
        }

        lock (gate)
        {
            probes[version] = new ProbeCacheEntry(selection);
        }

        return selection;
    }

    private static IReadOnlyList<string> ProbeArguments(string sourcePath) =>
    [
        "-v",
        "error",
        "-select_streams",
        "a",
        "-show_entries",
        "stream=codec_type,codec_name,disposition=default",
        "-of",
        "json",
        sourcePath,
    ];

    private static bool TryParseProbe(string json, out ProbeSelection selection)
    {
        selection = new ProbeSelection(-1, string.Empty);
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaxProbeOutputBytes)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("streams", out var streams)
                || streams.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var ordinal = -1;
            var selected = -1;
            string? codec = null;
            var hasDefault = false;
            foreach (var stream in streams.EnumerateArray())
            {
                if (!IsAudioStream(stream))
                {
                    continue;
                }

                if (!TryReadCodecName(stream, out var currentCodec))
                {
                    return false;
                }

                ordinal++;
                var isDefault = TryReadDefaultFlag(stream);
                if (isDefault && !hasDefault)
                {
                    selected = ordinal;
                    codec = currentCodec;
                    hasDefault = true;
                }
                else if (selected < 0)
                {
                    selected = ordinal;
                    codec = currentCodec;
                }
            }

            if (selected < 0 || codec is null)
            {
                selection = new ProbeSelection(-1, string.Empty);
                return true;
            }

            selection = new ProbeSelection(selected, codec);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsAudioStream(JsonElement stream) =>
        stream.ValueKind == JsonValueKind.Object
        && stream.TryGetProperty("codec_type", out var type)
        && type.ValueKind == JsonValueKind.String
        && type.GetString() == "audio";

    private static bool TryReadCodecName(JsonElement stream, out string codec)
    {
        codec = string.Empty;
        if (!stream.TryGetProperty("codec_name", out var name)
            || name.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var value = name.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        codec = value.ToLowerInvariant();
        return true;
    }

    private static bool TryReadDefaultFlag(JsonElement stream)
    {
        if (!stream.TryGetProperty("disposition", out var disposition)
            || disposition.ValueKind != JsonValueKind.Object
            || !disposition.TryGetProperty("default", out var flag))
        {
            return false;
        }

        return flag.ValueKind switch
        {
            JsonValueKind.Number => flag.TryGetInt32(out var value) && value != 0,
            JsonValueKind.True => true,
            _ => false,
        };
    }

    private string ResolveFfprobePath()
    {
        var directory = Path.GetDirectoryName(thumbnailOptions.FfmpegPath);
        if (string.IsNullOrEmpty(directory))
        {
            return "ffprobe";
        }

        return Path.Combine(directory, "ffprobe" + Path.GetExtension(thumbnailOptions.FfmpegPath));
    }
}
