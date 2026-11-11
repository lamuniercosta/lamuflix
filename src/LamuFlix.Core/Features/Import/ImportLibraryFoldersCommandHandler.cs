using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Import;

public sealed class ImportLibraryFoldersCommandHandler(
    IMovieFolderImporter importer,
    IMovieRepository movies,
    IEnrichmentOutbox outbox,
    IOptions<LibraryOptions> libraryOptions)
    : ICommandHandler<ImportLibraryFoldersCommand, ImportLibraryFoldersResult>
{
    public async Task<ImportLibraryFoldersResult> HandleAsync(
        ImportLibraryFoldersCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var snapshot = await movies.SnapshotMoviePathsAsync(cancellationToken);
        var importedPaths = ImportedPathSet.FromPaths(snapshot);

        var imported = 0;
        var skipped = 0;
        var retried = 0;
        var failures = new List<ImportLibraryFolderFailure>();

        foreach (var folder in command.Folders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!LibraryPath.TryCanonicalizeUnderRoot(folder, libraryOptions.Value.RootPath, out var canonical))
            {
                failures.Add(new ImportLibraryFolderFailure(folder, ImportFolderFailureReason.InvalidFolder));
                continue;
            }

            var path = new LibraryPath(canonical);

            if (importedPaths.Contains(path.Value))
            {
                skipped++;
                continue;
            }

            try
            {
                await importer.ImportAsync(path, cancellationToken);
                imported++;
                importedPaths.Add(path.Value);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (await IsImportedAfterFaultAsync(movies, importedPaths, path, cancellationToken))
                {
                    skipped++;
                    continue;
                }

                failures.Add(new ImportLibraryFolderFailure(path.Value, MapReason(exception)));
            }
        }

        if (command.RetryFailed)
        {
            var failed = await movies.FindFailedMoviesAsync(cancellationToken);
            foreach (var movie in failed)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ReferenceEquals(movie.Status, EnrichmentStatus.Failed))
                {
                    continue;
                }

                movie.RequestEnrichment();
                outbox.Stage(new EnrichmentRequested(movie.Id, 1));
                retried++;
            }

            await movies.SaveChangesAsync(cancellationToken);
        }

        return new ImportLibraryFoldersResult(imported, skipped, retried, [.. failures]);
    }

    private static async Task<bool> IsImportedAfterFaultAsync(
        IMovieRepository movies,
        HashSet<string> importedPaths,
        LibraryPath path,
        CancellationToken cancellationToken)
    {
        var snapshot = await movies.SnapshotMoviePathsAsync(cancellationToken);
        importedPaths.Clear();
        foreach (var stored in snapshot)
        {
            importedPaths.Add(stored.Value);
        }

        return importedPaths.Contains(path.Value);
    }

    private static ImportFolderFailureReason MapReason(Exception exception) => exception switch
    {
        NotFoundException => ImportFolderFailureReason.FolderNotFound,
        InvalidTransitionException => ImportFolderFailureReason.NoVideo,
        ValidationException => ImportFolderFailureReason.InvalidFolder,
        _ => ImportFolderFailureReason.ImportFailed,
    };
}
