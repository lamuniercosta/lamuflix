namespace LamuFlix.Core.Features.Enrichment;

public sealed record BackfillTmdbRecommendationsResult(
    int ImdbEligible,
    int Matched,
    int Unmatched,
    int Skipped,
    int Failures,
    BackfillStopReason? StopReason = null)
{
    public bool Stopped => StopReason is not null;
}

public enum BackfillStopReason
{
    ProviderUnavailable,
    Cancelled,
}
