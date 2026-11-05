using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Infrastructure.Playback;

public sealed class DisabledMediaPlayerLauncher : IMediaPlayerLauncher
{
    private const string Message = "LocalPlay is disabled in this environment.";

    public void EnsureEnabled() => throw new FeatureDisabledException(Message);

    public void Launch(LibraryPath file, MediaFormat format) => throw new FeatureDisabledException(Message);
}
