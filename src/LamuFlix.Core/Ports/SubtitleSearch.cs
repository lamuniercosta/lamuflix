using System.Collections.Generic;

namespace LamuFlix.Core.Ports;

public sealed record SubtitleSearch(
    string? MovieHash,
    long FileSize,
    string? Title,
    IReadOnlyList<string> PreferredLanguages);
