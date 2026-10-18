using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Feedback;

public sealed class MarkNotInterestedCommandHandler(IMovieRepository movies)
    : ICommandHandler<MarkNotInterestedCommand, Unit>
{
    public async Task<Unit> HandleAsync(MarkNotInterestedCommand command, CancellationToken cancellationToken)
    {
        var movie = await movies.GetAsync(command.Id, cancellationToken)
                    ?? throw new NotFoundException();
        movie.MarkNotInterested();
        await movies.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
