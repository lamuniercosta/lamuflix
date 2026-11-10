using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Import;

public sealed class ImportLibraryFoldersCommandHandler(
    IMovieFolderImporter importer,
    IMovieRepository movies,
    IEnrichmentOutbox outbox)
    : ICommandHandler<ImportLibraryFoldersCommand, ImportLibraryFoldersResult>
{
    private const string FolderNotFoundReason = "folder-not-found";
    private const string NoVideoReason = "no-video";
    private const string InvalidFolderReason = "invalid-folder";
    private const string ImportFailedReason = "import-failed";

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

        foreach (var folder in command.Folders ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (importedPaths.Contains(folder))
            {
                skipped++;
                continue;
            }

            try
            {
                await importer.ImportAsync(new LibraryPath(folder), cancellationToken);
                imported++;
                importedPaths.Add(folder);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failures.Add(new ImportLibraryFolderFailure(new LibraryPath(folder), MapReason(exception)));
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

        return new ImportLibraryFoldersResult(imported, skipped, retried, failures);
    }

    private static string MapReason(Exception exception) => exception switch
    {
        NotFoundException => FolderNotFoundReason,
        InvalidTransitionException => NoVideoReason,
        ValidationException => InvalidFolderReason,
        _ => ImportFailedReason,
    };
}
