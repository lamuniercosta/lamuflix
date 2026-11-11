using System;
using System.Collections.Generic;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Subtitles;

internal static class SubtitlePayloadErrors
{
    private const string TrackKey = "Track";
    private const string UnavailableMessage = "The subtitle track is not a supported subtitle document.";

    internal static Dictionary<string, string[]> Create() =>
        new(StringComparer.Ordinal) { [TrackKey] = [UnavailableMessage] };

    internal static ValidationException Invalid() => new(Create());
}
