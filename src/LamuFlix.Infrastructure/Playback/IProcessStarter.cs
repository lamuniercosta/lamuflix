using System.Diagnostics;

namespace LamuFlix.Infrastructure.Playback;

public interface IProcessStarter
{
    void Start(ProcessStartInfo startInfo);
}
