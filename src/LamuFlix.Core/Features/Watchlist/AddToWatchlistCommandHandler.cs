using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Watchlist;

public sealed class AddToWatchlistCommandHandler(IMovieRepository movies)
    : ICommandHandler<AddToWatchlistCommand, Unit>
{
    public async Task<Unit> HandleAsync(AddToWatchlistCommand command, CancellationToken cancellationToken)
    {
        var movie = await movies.GetAsync(command.Id, cancellationToken)
                    ?? throw new NotFoundException();
        movie.AddToWatchlist();
        await movies.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
