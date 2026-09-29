using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record MovieSummary(MovieId Id, string Title)
{
    public override string ToString() => $"{nameof(MovieSummary)} {{ {nameof(Id)} = {Id}, {nameof(Title)} = {Title} }}";
}
