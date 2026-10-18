using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Feedback;

public sealed class UnmarkWatchedCommandHandler(IMovieRepository movies)
    : ICommandHandler<UnmarkWatchedCommand, Unit>
{
    public async Task<Unit> HandleAsync(UnmarkWatchedCommand command, CancellationToken cancellationToken)
    {
        var movie = await movies.GetAsync(command.Id, cancellationToken)
                    ?? throw new NotFoundException();
        movie.UnmarkWatched();
        await movies.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
