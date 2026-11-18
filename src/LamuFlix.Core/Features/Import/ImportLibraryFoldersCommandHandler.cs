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
        var failures = new List<ImportLibraryFolderFailure>();

        foreach (var folder in command.Folders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await ImportFolderAsync(folder, importedPaths, cancellationToken);
            if (result.Outcome == FolderImportOutcome.Imported)
            {
                imported++;
            }
            else if (result.Outcome == FolderImportOutcome.Skipped)
            {
                skipped++;
            }
            else if (result.Failure is not null)
            {
                failures.Add(result.Failure);
            }
        }

        var retried = 0;
        if (command.RetryFailed)
        {
            retried = await RetryFailedAsync(cancellationToken);
        }

        return new ImportLibraryFoldersResult(imported, skipped, retried, [.. failures]);
    }

    private async Task<FolderImportResult> ImportFolderAsync(
        string folder,
        HashSet<string> importedPaths,
        CancellationToken cancellationToken)
    {
        if (!LibraryPath.TryCanonicalizeUnderRoot(folder, libraryOptions.Value.RootPath, out var canonical))
        {
            return new FolderImportResult(FolderImportOutcome.Failed, new ImportLibraryFolderFailure(folder, ImportFolderFailureReason.InvalidFolder));
        }

        var path = new LibraryPath(canonical);

        if (importedPaths.Contains(path.Value))
        {
            return new FolderImportResult(FolderImportOutcome.Skipped);
        }

        return await AttemptImportAsync(path, importedPaths, cancellationToken);
    }

    private async Task<FolderImportResult> AttemptImportAsync(
        LibraryPath path,
        HashSet<string> importedPaths,
        CancellationToken cancellationToken)
    {
        try
        {
            await importer.ImportAsync(path, cancellationToken);
            importedPaths.Add(path.Value);
            return new FolderImportResult(FolderImportOutcome.Imported);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DuplicateLibraryPathException)
        {
            importedPaths.Add(path.Value);
            return new FolderImportResult(FolderImportOutcome.Skipped);
        }
        catch (Exception exception)
        {
            if (await IsImportedAfterFaultAsync(movies, importedPaths, path, cancellationToken))
            {
                return new FolderImportResult(FolderImportOutcome.Skipped);
            }

            return new FolderImportResult(FolderImportOutcome.Failed, new ImportLibraryFolderFailure(path.Value, MapReason(exception)));
        }
    }

    private async Task<int> RetryFailedAsync(CancellationToken cancellationToken)
    {
        var retried = 0;
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
        return retried;
    }

    private enum FolderImportOutcome
    {
        Imported,
        Skipped,
        Failed,
    }

    private sealed record FolderImportResult(FolderImportOutcome Outcome, ImportLibraryFolderFailure? Failure = null);

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
