using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public abstract record MetadataLookupResult
{
    private MetadataLookupResult()
    {
    }

    public sealed record Found(MovieMetadata Metadata) : MetadataLookupResult
    {
        public override string ToString() => $"{nameof(Found)} {{ {nameof(Metadata)} = {Metadata} }}";
    }

    public sealed record NotFound : MetadataLookupResult;

    public sealed record Failed(EnrichmentFailureCategory Category) : MetadataLookupResult
    {
        public override string ToString() => $"{nameof(Failed)} {{ {nameof(Category)} = {Category} }}";
    }
}
