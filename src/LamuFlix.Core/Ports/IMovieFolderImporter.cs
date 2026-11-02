using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

/// <summary>
/// Imports one Candidate Movie Folder through a fresh per-folder scope.
/// </summary>
public interface IMovieFolderImporter
{
    Task<MovieId> ImportAsync(LibraryPath folder, CancellationToken cancellationToken);
}
