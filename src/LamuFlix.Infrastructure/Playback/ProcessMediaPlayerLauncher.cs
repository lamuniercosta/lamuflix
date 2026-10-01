using System;
using System.Diagnostics;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Playback;

public sealed class ProcessMediaPlayerLauncher(IOptions<PlaybackOptions> options, IProcessStarter starter)
    : IMediaPlayerLauncher
{
    public void Launch(LibraryPath file, MediaFormat format)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(starter);

        var startInfo = new ProcessStartInfo { UseShellExecute = true };
        var executable = ResolveExecutable(format);
        if (executable is null)
        {
            startInfo.FileName = file.Value;
        }
        else
        {
            startInfo.FileName = executable;
            startInfo.ArgumentList.Add(file.Value);
        }

        starter.Start(startInfo);
    }

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
