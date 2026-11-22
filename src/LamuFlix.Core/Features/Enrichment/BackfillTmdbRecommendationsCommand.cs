namespace LamuFlix.Core.Features.Enrichment;

/// <summary>
/// The one-off operator backfill for IMDb-identified movies that still lack a TMDb identity.
/// The invocation carries no payload: eligibility, configuration and cancellation come from the run.
/// </summary>
public sealed record BackfillTmdbRecommendationsCommand;
