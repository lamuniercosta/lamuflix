using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class RequeueStrandedMoviesCommandHandler(IEnrichmentQueue queue)
    : ICommandHandler<RequeueStrandedMoviesCommand, int>
{
    public async Task<int> HandleAsync(RequeueStrandedMoviesCommand command, CancellationToken cancellationToken)
    {
        var count = 0;
        foreach (var id in command.MovieIds)
        {
            await queue.EnqueueAsync(new EnrichmentRequested(id, 1), cancellationToken);
            count++;
        }

        return count;
    }
}
