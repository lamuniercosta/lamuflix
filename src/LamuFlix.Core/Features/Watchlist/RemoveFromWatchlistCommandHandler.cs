using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Watchlist;

public sealed class RemoveFromWatchlistCommandHandler(IMovieRepository movies)
    : ICommandHandler<RemoveFromWatchlistCommand, Unit>
{
    public async Task<Unit> HandleAsync(RemoveFromWatchlistCommand command, CancellationToken cancellationToken)
    {
        var movie = await movies.GetAsync(command.Id, cancellationToken)
                    ?? throw new NotFoundException();
        movie.RemoveFromWatchlist();
        await movies.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
