using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Library;

public sealed class DeleteMovieCommandHandler(IMovieRepository movies)
    : ICommandHandler<DeleteMovieCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteMovieCommand command, CancellationToken cancellationToken)
    {
        var removed = await movies.TryDeleteAsync(command.Id, cancellationToken);
        if (!removed)
        {
            throw new NotFoundException();
        }

        return Unit.Value;
    }
}
