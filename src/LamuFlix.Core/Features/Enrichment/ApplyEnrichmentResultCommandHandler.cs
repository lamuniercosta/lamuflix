using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class ApplyEnrichmentResultCommandHandler(IMovieRepository movies, TimeProvider time)
    : ICommandHandler<ApplyEnrichmentResultCommand, EnrichmentStatus>
{
    public async Task<EnrichmentStatus> HandleAsync(
        ApplyEnrichmentResultCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Result is MetadataLookupResult.Failed)
        {
            throw new ArgumentException("Failed lookup results cannot be applied.", nameof(command));
        }

        var movie = await movies.GetAsync(command.Id, cancellationToken)
                    ?? throw new NotFoundException();
        var now = time.GetUtcNow();
        Apply(movie, command.Result, now);
        await movies.SaveChangesAsync(cancellationToken);
        return movie.Status;
    }

    private static void Apply(Movie movie, MetadataLookupResult result, DateTimeOffset now)
    {
        if (result is MetadataLookupResult.Found found)
        {
            movie.MarkEnriched(found.Metadata, now);
            return;
        }

        movie.MarkNotFound(now);
    }
}
