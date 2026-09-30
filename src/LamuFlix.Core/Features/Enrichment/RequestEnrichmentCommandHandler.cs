using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class RequestEnrichmentCommandHandler(IMovieRepository movies, IEnrichmentQueue queue)
    : ICommandHandler<RequestEnrichmentCommand, MovieId>
{
    public async Task<MovieId> HandleAsync(RequestEnrichmentCommand command, CancellationToken cancellationToken)
    {
        var movie = await movies.GetAsync(command.Id, cancellationToken)
                    ?? throw new NotFoundException();
        if (!ReferenceEquals(movie.Status, EnrichmentStatus.NotFound)
            && !ReferenceEquals(movie.Status, EnrichmentStatus.Failed))
        {
            throw new InvalidTransitionException(nameof(Movie.RequestEnrichment), movie.Status.ToString());
        }

        movie.RequestEnrichment();
        await movies.SaveChangesAsync(cancellationToken);
        await queue.EnqueueAsync(new EnrichmentRequested(command.Id, 1), cancellationToken);
        return command.Id;
    }
}
