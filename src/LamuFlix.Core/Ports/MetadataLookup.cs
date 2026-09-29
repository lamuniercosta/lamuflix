using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record MetadataLookup(string Title, ReleaseYear? ReleaseYear)
{
    public override string ToString() => $"{nameof(MetadataLookup)} {{ {nameof(Title)} = {Title}, {nameof(ReleaseYear)} = {ReleaseYear} }}";
}
