namespace LamuFlix.Core.Domain;

public sealed record EnrichmentFailureDecision(EnrichmentFailureAction Action, int? NextAttempt);
