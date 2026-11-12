using System.Collections.Immutable;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record SubtitleSearchCandidate(
    string CandidateId,
    string Language,
    string ReleaseName,
    int MatchPercent,
    int DownloadCount);

public sealed record SearchSubtitlesResult(
    ImmutableArray<SubtitleSearchCandidate> Candidates,
    SubtitleQuota Quota,
    bool DownloadEnabled,
    string DownloadReason);
