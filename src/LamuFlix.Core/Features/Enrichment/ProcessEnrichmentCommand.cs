using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Enrichment;

public sealed record ProcessEnrichmentCommand(MovieId MovieId, int Attempt);