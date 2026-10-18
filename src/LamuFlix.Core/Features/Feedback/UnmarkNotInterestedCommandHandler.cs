using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Feedback;

public sealed class UnmarkNotInterestedCommandHandler(IMovieRepository movies)
    : ICommandHandler<UnmarkNotInterestedCommand, Unit>
{
    public async Task<Unit> HandleAsync(UnmarkNotInterestedCommand command, CancellationToken cancellationToken)
    {
        var movie = await movies.GetAsync(command.Id, cancellationToken)
                    ?? throw new NotFoundException();
        movie.UnmarkNotInterested();
        await movies.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
