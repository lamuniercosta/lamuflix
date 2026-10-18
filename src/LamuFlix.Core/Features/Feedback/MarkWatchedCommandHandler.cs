using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Feedback;

public sealed class MarkWatchedCommandHandler(IMovieRepository movies)
    : ICommandHandler<MarkWatchedCommand, Unit>
{
    public async Task<Unit> HandleAsync(MarkWatchedCommand command, CancellationToken cancellationToken)
    {
        var movie = await movies.GetAsync(command.Id, cancellationToken)
                    ?? throw new NotFoundException();
        movie.MarkWatched();
        await movies.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
