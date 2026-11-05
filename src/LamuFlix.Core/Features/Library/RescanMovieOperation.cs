using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Library;

internal sealed class RescanMovieOperation(IMovieRepository movies, IMediaLibraryScanner scanner)
{
    public async Task<RescanMovieResult> ExecuteAsync(MovieId id, CancellationToken cancellationToken)
    {
        var result = await PrepareAsync(id, cancellationToken);
        if (!result.Changed)
        {
            return result;
        }

        await SaveAsync(cancellationToken);
        return result;
    }

    public async Task<RescanMovieResult> PrepareAsync(MovieId id, CancellationToken cancellationToken)
    {
        var movie = await movies.GetAsync(id, cancellationToken) ?? throw new NotFoundException();
        var oldFormat = movie.Format;
        cancellationToken.ThrowIfCancellationRequested();
        var primary = scanner.ResolvePrimaryVideo(movie.Path);
        if (oldFormat == primary.Format)
        {
            return new RescanMovieResult(id, false, oldFormat, primary.Format);
        }

        movie.RefreshFormat(primary.Format);
        cancellationToken.ThrowIfCancellationRequested();
        return new RescanMovieResult(id, true, oldFormat, primary.Format);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => movies.SaveChangesAsync(cancellationToken);
}
