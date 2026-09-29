using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

// ReSharper disable NotAccessedPositionalProperty.Global
// Consumed by future enrichment queue and worker adapters
public sealed record EnrichmentRequested(MovieId MovieId, int Attempt);
// ReSharper restore NotAccessedPositionalProperty.Global
