using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record EnrichmentRequested(MovieId MovieId, int Attempt);
