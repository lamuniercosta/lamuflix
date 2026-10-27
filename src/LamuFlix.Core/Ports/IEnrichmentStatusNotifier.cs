using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface IEnrichmentStatusNotifier
{
    Task NotifyAsync(MovieId movieId, EnrichmentStatus status, CancellationToken cancellationToken);
}
