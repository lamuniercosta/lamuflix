using System.Collections.Immutable;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record ListSubtitleTracksResult(ImmutableList<SubtitleTrack> Tracks);
