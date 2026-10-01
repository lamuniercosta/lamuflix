using System.Threading;
using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.Adapters;

public sealed class MetadataProviderHealthState
{
    private Snapshot? snapshot;

    internal void Record(EnrichmentFailureCategory? category, bool wasUnauthorized) =>
        Volatile.Write(ref snapshot, new Snapshot(category, wasUnauthorized));

    internal Snapshot? Current => Volatile.Read(ref snapshot);

    internal sealed record Snapshot(EnrichmentFailureCategory? Category, bool WasUnauthorized);
}