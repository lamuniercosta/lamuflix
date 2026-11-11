namespace LamuFlix.Core.Ports;

public sealed record SubtitleCandidate(long FileId, string Language, int DownloadCount, bool MatchesMovieHash);
