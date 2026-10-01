using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Infrastructure.Playback;

public sealed class DisabledMediaPlayerLauncher : IMediaPlayerLauncher
{
    public void Launch(LibraryPath file, MediaFormat format) =>
        throw new FeatureDisabledException("LocalPlay is disabled in this environment.");
}
