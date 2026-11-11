using System;
using LamuFlix.Core.Features.Subtitles;

namespace LamuFlix.Api.Endpoints;

// ReSharper disable NotAccessedPositionalProperty.Global // Serialized by System.Text.Json as the frozen wire shape, never read in code
internal sealed record SubtitlePrepareResponse(string Outcome, string? SelectedLanguage)
{
    public static SubtitlePrepareResponse From(SubtitlePreparationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new SubtitlePrepareResponse(result.Outcome.Code, result.SelectedLanguage);
    }
}
// ReSharper restore NotAccessedPositionalProperty.Global
