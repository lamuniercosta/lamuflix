using System.Collections.Generic;
using System.Threading;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

/// <summary>
/// Streams the Candidate Movie Folders at or below the configured library root.
/// </summary>
public interface ILibraryFolderDiscovery
{
    IAsyncEnumerable<LibraryPath> DiscoverAsync(CancellationToken cancellationToken);
}
