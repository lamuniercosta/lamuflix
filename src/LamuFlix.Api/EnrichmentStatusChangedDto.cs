using LamuFlix.Core.Domain;

namespace LamuFlix.Api;

public sealed record EnrichmentStatusChangedDto(int Id, EnrichmentStatus EnrichmentStatus);
