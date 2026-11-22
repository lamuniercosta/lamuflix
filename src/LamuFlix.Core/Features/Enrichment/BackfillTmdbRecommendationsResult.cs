namespace LamuFlix.Core.Features.Enrichment;

/// <summary>
/// The five counts reported by the one-off TMDb backfill, plus the reason a run stopped early.
/// <see cref="ImdbEligible"/> counts movies holding an IMDb id; <see cref="Skipped"/> counts movies without
/// one and movies that already store a TMDb id. A stopped run keeps the counts it had reached.
/// </summary>
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

/// <summary>
/// Why a backfill run stopped before reaching the end of the library.
/// The shared resilience pipeline reports a broken circuit as <see cref="ProviderUnavailable"/>.
/// </summary>
public enum BackfillStopReason
{
    ProviderUnavailable,
    Cancelled,
}
