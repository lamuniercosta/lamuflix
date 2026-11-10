using System.Collections.Generic;
using System.IO;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface ILibraryFileReader
{
    void AuthorizeFolder(LibraryPath folder);

    Stream OpenRead(LibraryPath file);

    /// <summary>
    /// Authorizes <paramref name="folder"/> exactly like <see cref="AuthorizeFolder"/> and returns the
    /// exact file names of the regular files directly inside it, in ordinal order. Entries that are not
    /// regular files (links, reparse points, directories, special files) are never listed, and the
    /// enumeration never recurses into subdirectories.
    /// </summary>
    IReadOnlyList<string> EnumerateFiles(LibraryPath folder);
}
