using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

// ReSharper disable NotAccessedPositionalProperty.Global
// Consumed by future metadata provider adapters
public sealed record MetadataLookup(string Title, ReleaseYear? ReleaseYear, ImdbId? ManualImdbId = null);
// ReSharper restore NotAccessedPositionalProperty.Global
