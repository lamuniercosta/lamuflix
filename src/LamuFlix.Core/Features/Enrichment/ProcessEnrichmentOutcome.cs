using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Enrichment;

public abstract record ProcessEnrichmentOutcome
{
    private ProcessEnrichmentOutcome()
    {
    }

    // ReSharper disable NotAccessedPositionalProperty.Global
    // Claimed is false when another attempt holds the lease, which makes this a skip, not an enrichment.
    public sealed record Completed(bool Claimed) : ProcessEnrichmentOutcome;
    // ReSharper restore NotAccessedPositionalProperty.Global

    // ReSharper disable NotAccessedPositionalProperty.Global
    public sealed record Failed(EnrichmentFailureDecision Decision) : ProcessEnrichmentOutcome;
    // ReSharper restore NotAccessedPositionalProperty.Global
}