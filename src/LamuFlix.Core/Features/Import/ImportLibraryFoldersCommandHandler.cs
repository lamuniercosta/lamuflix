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
        var importedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in snapshot)
        {
            importedPaths.Add(path.Value);
        }

        var imported = 0;
        var skipped = 0;
        var retried = 0;
        var failures = new List<ImportLibraryFolderFailure>();

        foreach (var folder in command.Folders)
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
                EnrichmentMetrics.ImportCount.Add(1);
                imported++;
            }
            catch (Exception exception)
            {
                failures.Add(new ImportLibraryFolderFailure(new LibraryPath(folder), MapReason(exception)));
            }
        }

        if (command.RetryFailed)
        {
            var failedIds = await movies.FindFailedMovieIdsAsync(cancellationToken);
            foreach (var id in failedIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var movie = await movies.GetAsync(id, cancellationToken);
                if (movie is null)
                {
                    continue;
                }

                if (!ReferenceEquals(movie.Status, EnrichmentStatus.NotFound)
                    && !ReferenceEquals(movie.Status, EnrichmentStatus.Failed))
                {
                    continue;
                }

                movie.RequestEnrichment();
                outbox.Stage(new EnrichmentRequested(id, 1));
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
