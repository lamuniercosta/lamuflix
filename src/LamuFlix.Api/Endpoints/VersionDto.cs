namespace LamuFlix.Api.Endpoints;

// ReSharper disable once NotAccessedPositionalProperty.Global - Serialized by System.Text.Json as the frozen wire shape, never read in code
internal sealed record VersionDto(
    string? Release,
    // ReSharper disable once NotAccessedPositionalProperty.Global - Serialized by System.Text.Json as the frozen wire shape, never read in code
    string Commit);
