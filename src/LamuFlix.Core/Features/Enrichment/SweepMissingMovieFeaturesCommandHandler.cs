using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class SweepMissingMovieFeaturesCommandHandler(
    IMovieFeaturesRepository features,
    IEnrichmentQueue queue)
    : ICommandHandler<SweepMissingMovieFeaturesCommand, int>
{
    public async Task<int> HandleAsync(
        SweepMissingMovieFeaturesCommand command,
        CancellationToken cancellationToken)
    {
        var missing = await features.FindMovieIdsMissingFeaturesAsync(cancellationToken);

        var count = 0;
        foreach (var movieId in missing)
        {
            await queue.EnqueueAsync(new FeaturesRequested(movieId, 1), cancellationToken);
            count++;
        }

        return count;
    }
}
