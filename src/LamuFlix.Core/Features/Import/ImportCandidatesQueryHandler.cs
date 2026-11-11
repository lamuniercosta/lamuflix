using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Import;

public sealed class ImportCandidatesQueryHandler(
    ILibraryFolderDiscovery discovery,
    IMovieRepository movies)
    : IQueryHandler<ImportCandidatesQuery, ImportCandidatesResult>
{
    public async Task<ImportCandidatesResult> HandleAsync(
        ImportCandidatesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var imported = await movies.SnapshotMoviePathsAsync(cancellationToken);
        var importedPaths = ImportedPathSet.FromPaths(imported);

        var newFolders = new List<LibraryPath>();
        await foreach (var folder in discovery.DiscoverAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (importedPaths.Contains(folder.Value))
            {
                continue;
            }

            newFolders.Add(folder);
        }

        return new ImportCandidatesResult([.. newFolders]);
    }
}
