using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class RequestEnrichmentCommandHandler(
    IMovieRepository movies,
    IEnrichmentOutbox outbox)
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
        outbox.Stage(new EnrichmentRequested(command.Id, 1));
        await movies.SaveChangesAsync(cancellationToken);
        return command.Id;
    }
}
