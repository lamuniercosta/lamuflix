namespace LamuFlix.Api.Endpoints;

// ReSharper disable NotAccessedPositionalProperty.Global // Serialized by System.Text.Json as the wire shape, never read in code
internal sealed record SubtitleLanguagesResponse(string? DefaultLanguage, SubtitleLanguageOptionDto[] Languages);
// ReSharper restore NotAccessedPositionalProperty.Global
