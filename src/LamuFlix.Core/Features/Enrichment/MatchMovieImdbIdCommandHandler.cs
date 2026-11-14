using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class MatchMovieImdbIdCommandHandler(
    IMovieRepository movies,
    IEnrichmentOutbox outbox)
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
        outbox.Stage(new EnrichmentRequested(command.Id, 1));
        try
        {
            await movies.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateImdbIdException)
        {
            if (await OtherHolderAsync(command, cancellationToken) is { } winner)
            {
                throw new ImdbIdConflictException(command.ImdbId, winner);
            }

            throw;
        }

        return command.Id;
    }

    private async Task<MovieId?> OtherHolderAsync(
        MatchMovieImdbIdCommand command,
        CancellationToken cancellationToken) =>
        await movies.FindMovieIdByImdbIdAsync(command.ImdbId, cancellationToken) is { } holder
        && holder != command.Id
            ? holder
            : null;
}
