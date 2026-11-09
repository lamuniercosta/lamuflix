using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Playback;

public sealed class ProcessMediaPlayerLauncher(
    IOptions<PlaybackOptions> options,
    IProcessStarter starter,
    IFileSystem fileSystem)
    : IMediaPlayerLauncher
{
    private const string FormatErrorKey = "format";

    public void EnsureEnabled()
    {
    }

    public void Launch(LibraryPath file, MediaFormat format)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(starter);
        ArgumentNullException.ThrowIfNull(fileSystem);

        if (!fileSystem.File.Exists(file.Value))
        {
            throw new NotFoundException();
        }

        var executable = ResolveExecutable(format) ?? throw UnmappedFormat(format);

        var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = executable };
        startInfo.ArgumentList.Add(file.Value);

        starter.Start(startInfo);
    }

    private static ValidationException UnmappedFormat(MediaFormat format) =>
        new(new Dictionary<string, string[]>
        {
            [FormatErrorKey] = [$"No configured player supports the '{format.Extension}' format."],
        });

    private string? ResolveExecutable(MediaFormat format)
    {
        foreach (var player in options.Value.Players)
        {
            if (string.IsNullOrWhiteSpace(player.ExecutablePath))
            {
                continue;
            }

            foreach (var raw in player.Formats)
            {
                if (MediaFormat.TryCreate(raw, out var parsed) && parsed.Extension == format.Extension)
                {
                    return player.ExecutablePath;
                }
            }
        }

        return null;
    }
}
