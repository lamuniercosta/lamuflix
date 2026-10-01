using System;
using System.Diagnostics;

namespace LamuFlix.Infrastructure.Playback;

public sealed class SystemProcessStarter : IProcessStarter
{
    public void Start(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        Process.Start(startInfo)?.Dispose();
    }
}
