using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record FeaturesRequested(MovieId MovieId, int Attempt);
