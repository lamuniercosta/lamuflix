using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class SweepStrandedMoviesCommandHandler(
    IMovieRepository movieRepository,
    IOptions<EnrichmentOptions> options,
    TimeProvider timeProvider,
    ICommandHandler<RequeueStrandedMoviesCommand, int> requeueHandler)
    : ICommandHandler<SweepStrandedMoviesCommand, int>
{
    public async Task<int> HandleAsync(SweepStrandedMoviesCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var leaseCutoff = now - options.Value.ClaimLease;
        var movieIds = await movieRepository.FindStrandedMovieIdsAsync(leaseCutoff, cancellationToken);
        if (movieIds.Count == 0)
        {
            return 0;
        }

        return await requeueHandler.HandleAsync(new RequeueStrandedMoviesCommand(movieIds), cancellationToken);
    }
}