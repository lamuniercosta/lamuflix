using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Import;

public sealed class ImportMovieFolderCommandHandler(
    IMediaLibraryScanner scanner,
    IMovieRepository movies,
    IEnrichmentOutbox outbox,
    IOptions<LibraryOptions> libraryOptions)
    : ICommandHandler<ImportMovieFolderCommand, MovieId>
{
    private const string FolderPathKey = "folderPath";

    public async Task<MovieId> HandleAsync(ImportMovieFolderCommand command, CancellationToken cancellationToken)
    {
        var folder = Canonicalize(command.Folder);
        await RejectWhenAlreadyImportedAsync(folder, cancellationToken);
        var scanned = scanner.Scan(folder);
        var id = await movies.NextIdentityAsync(cancellationToken);
        var movie = Movie.Create(id, scanned.Title, scanned.Path, scanned.Format, scanned.Year);
        await movies.AddAsync(movie, cancellationToken);
        outbox.Stage(new EnrichmentRequested(id, 1));
        await movies.SaveChangesAsync(cancellationToken);
        EnrichmentMetrics.ImportCount.Add(1);
        return id;
    }

    /// <summary>
    /// DEV-522 US2: a folder whose library path/key is already held is skipped before the scan, the
    /// identity allocation and <c>outbox.Stage</c>, so a repeated import can never queue a second
    /// enrichment. The outcome is the existing already-present conflict, and the stored row is reused
    /// as-is: nothing is merged, deleted or re-created. Import carries no IMDb identity (see T001
    /// evidence §2b), so a distinct path for an already-known movie is still discovered at enrichment.
    /// </summary>
    private async Task RejectWhenAlreadyImportedAsync(LibraryPath folder, CancellationToken cancellationToken)
    {
        var imported = await movies.SnapshotMoviePathsAsync(cancellationToken);
        if (ImportedPathSet.FromPaths(imported).Contains(folder.Value))
        {
            throw new DuplicateLibraryPathException();
        }
    }

    private LibraryPath Canonicalize(LibraryPath folder)
    {
        if (LibraryPath.TryCanonicalizeUnderRoot(folder.Value, libraryOptions.Value.RootPath, out var canonical))
        {
            return new LibraryPath(canonical);
        }

        throw new ValidationException(new Dictionary<string, string[]>
        {
            [FolderPathKey] =
            [
                "Folder path must be a library-relative folder or an absolute folder under the configured "
                + "Library:RootPath. Legacy library roots never authorize a new import.",
            ],
        });
    }
}
