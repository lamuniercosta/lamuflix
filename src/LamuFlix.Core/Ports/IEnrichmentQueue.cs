using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Core.Ports;

public interface IEnrichmentQueue
{
    Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct);

    Task EnqueueAsync(FeaturesRequested message, CancellationToken ct);
}
