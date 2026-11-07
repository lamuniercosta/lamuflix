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
    /// Maps a submitted relative identity or an absolute path beneath the active <c>Library:RootPath</c>
    /// to the canonical library-relative identity. Legacy root aliases never authorize a new import, and
    /// the process working directory is never consulted.
    /// </summary>
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
