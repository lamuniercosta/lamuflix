using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record DownloadSubtitleCommand(MovieId Id, string? CandidateId);
