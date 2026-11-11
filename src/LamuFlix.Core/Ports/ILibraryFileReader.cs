using System.Collections.Generic;
using System.IO;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface ILibraryFileReader
{
    void AuthorizeFolder(LibraryPath folder);

    Stream OpenRead(LibraryPath file);

    IReadOnlyList<string> EnumerateFiles(LibraryPath folder);
}
