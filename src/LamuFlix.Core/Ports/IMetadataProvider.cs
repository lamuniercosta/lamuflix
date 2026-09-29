using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Core.Ports;

public interface IMetadataProvider
{
    Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct);
}
