using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record MovieSummary(MovieId Id, string Title);
