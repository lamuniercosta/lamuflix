using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Enrichment;

public sealed record ApplyEnrichmentResultCommand(MovieId Id, MetadataLookupResult Result);
