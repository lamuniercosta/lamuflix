using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record GetSubtitleContentQuery(MovieId Id, string TrackId);
