using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Import;

public sealed class ImportMovieFolderCommandHandler(
    IMediaLibraryScanner scanner,
    IMovieRepository movies,
    IEnrichmentQueue queue)
    : ICommandHandler<ImportMovieFolderCommand, MovieId>
{
    public async Task<MovieId> HandleAsync(ImportMovieFolderCommand command, CancellationToken cancellationToken)
    {
        var scanned = scanner.Scan(command.Folder);
        var id = await movies.NextIdentityAsync(cancellationToken);
        var movie = Movie.Create(id, scanned.Title, scanned.Path, scanned.Format);
        await movies.AddAsync(movie, cancellationToken);
        await movies.SaveChangesAsync(cancellationToken);
        await queue.EnqueueAsync(new EnrichmentRequested(id, 1), cancellationToken);
        return id;
    }
}
