using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Enrichment;

public sealed record RecordEnrichmentFailureCommand(MovieId Id, int Attempt, EnrichmentFailureCategory Category);
