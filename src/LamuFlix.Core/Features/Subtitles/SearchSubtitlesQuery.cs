using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record SearchSubtitlesQuery(MovieId Id, string? Language);
