using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Enrichment;

public abstract record ProcessFeaturesOutcome
{
    private ProcessFeaturesOutcome()
    {
    }

    // ReSharper disable NotAccessedPositionalProperty.Global
    // Generated is false when the movie or plot was missing/blank, which makes this a successful no-op.
    public sealed record Completed(bool Generated) : ProcessFeaturesOutcome;
    // ReSharper restore NotAccessedPositionalProperty.Global

    // ReSharper disable NotAccessedPositionalProperty.Global
    public sealed record Failed(EnrichmentFailureDecision Decision, EnrichmentFailureCategory Category) : ProcessFeaturesOutcome;
    // ReSharper restore NotAccessedPositionalProperty.Global
}
