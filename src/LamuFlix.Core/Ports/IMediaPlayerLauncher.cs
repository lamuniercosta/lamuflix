using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface IMediaPlayerLauncher
{
    void EnsureEnabled();

    void Launch(LibraryPath file, MediaFormat format);
}
