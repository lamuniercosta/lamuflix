using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface IMediaLibraryScanner
{
    ScannedMovie Scan(LibraryPath folder);
}
