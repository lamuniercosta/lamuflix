using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record EnrichmentRequested(MovieId MovieId, int Attempt)
{
    public override string ToString() => $"{nameof(EnrichmentRequested)} {{ {nameof(MovieId)} = {MovieId}, {nameof(Attempt)} = {Attempt} }}";
}
