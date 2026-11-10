using System;
using System.Collections.Generic;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Subtitles;

/// <summary>
/// One sanitized <see cref="ValidationException"/> payload for unreadable or unusable subtitle content.
/// The message never names a path, a file name, or any cue text.
/// </summary>
internal static class SubtitlePayloadErrors
{
    private const string TrackKey = "Track";
    private const string UnavailableMessage = "The subtitle track is not a supported subtitle document.";

    internal static Dictionary<string, string[]> Create() =>
        new(StringComparer.Ordinal) { [TrackKey] = [UnavailableMessage] };

    internal static ValidationException Invalid() => new(Create());
}
