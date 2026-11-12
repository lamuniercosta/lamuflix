namespace LamuFlix.Core.Ports;

public sealed record SubtitleCandidate(
    long FileId,
    string Language,
    int DownloadCount,
    bool MatchesMovieHash,
    string? ReleaseName = null);

public sealed record SubtitleQuota(int? RemainingDownloads, System.DateTimeOffset? ResetAtUtc)
{
    public static readonly SubtitleQuota Unknown = new(RemainingDownloads: null, ResetAtUtc: null);

    public bool IsExhausted => RemainingDownloads is <= 0;
}

public sealed record SubtitleSearchResult(
    System.Collections.Generic.IReadOnlyList<SubtitleCandidate> Candidates,
    SubtitleQuota Quota);

public sealed record SubtitleDownload(byte[] Content, SubtitleQuota Quota);
