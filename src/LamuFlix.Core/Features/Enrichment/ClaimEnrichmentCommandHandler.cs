using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class ClaimEnrichmentCommandHandler(IMovieRepository movies)
    : ICommandHandler<ClaimEnrichmentCommand, bool>
{
    public Task<bool> HandleAsync(ClaimEnrichmentCommand command, CancellationToken cancellationToken) =>
        movies.TryClaimForEnrichmentAsync(command.Id, cancellationToken);
}
