using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record MetadataLookup(string Title, ReleaseYear? ReleaseYear);
