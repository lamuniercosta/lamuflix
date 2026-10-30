using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Core.Ports;

public interface IOmdbRequestGate
{
    Task<bool> TryAcquireAsync(CancellationToken cancellationToken);
}
