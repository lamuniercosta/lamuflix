using System.IO;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

/// <summary>
/// Authorizes Movie Folders and movie files under the configured library root and opens an
/// authorized file once for reading.
/// </summary>
public interface ILibraryFileReader
{
    /// <summary>Authorizes <paramref name="folder"/> as contained in the library root.</summary>
    void AuthorizeFolder(LibraryPath folder);

    /// <summary>Authorizes <paramref name="file"/> and returns one readable stream on it.</summary>
    Stream OpenRead(LibraryPath file);
}
