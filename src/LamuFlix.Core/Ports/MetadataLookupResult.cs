using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public abstract record MetadataLookupResult
{
    private MetadataLookupResult()
    {
    }

    // ReSharper disable NotAccessedPositionalProperty.Global
    // Consumed by future metadata provider adapters
    public sealed record Found(MovieMetadata Metadata) : MetadataLookupResult;
    // ReSharper restore NotAccessedPositionalProperty.Global

    public sealed record NotFound : MetadataLookupResult;

    // ReSharper disable NotAccessedPositionalProperty.Global
    // Consumed by future metadata provider adapters
    public sealed record Failed(EnrichmentFailureCategory Category) : MetadataLookupResult;
    // ReSharper restore NotAccessedPositionalProperty.Global
}
