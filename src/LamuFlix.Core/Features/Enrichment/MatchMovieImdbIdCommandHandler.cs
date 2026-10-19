using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class MatchMovieImdbIdCommandHandler(IMovieRepository movies, IEnrichmentQueue queue)
    : ICommandHandler<MatchMovieImdbIdCommand, MovieId>
{
    public async Task<MovieId> HandleAsync(MatchMovieImdbIdCommand command, CancellationToken cancellationToken)
    {
        var movie = await movies.GetAsync(command.Id, cancellationToken)
                    ?? throw new NotFoundException();

        var holder = await movies.FindMovieIdByImdbIdAsync(command.ImdbId, cancellationToken);
        if (holder is { } heldBy && heldBy != command.Id)
        {
            throw new ImdbIdConflictException(command.ImdbId, heldBy);
        }

        movie.MatchManually(command.ImdbId);
        await movies.SaveChangesAsync(cancellationToken);
        await queue.EnqueueAsync(new EnrichmentRequested(command.Id, 1), cancellationToken);
        return command.Id;
    }
}
