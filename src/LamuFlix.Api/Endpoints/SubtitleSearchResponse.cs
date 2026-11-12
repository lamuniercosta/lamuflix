namespace LamuFlix.Api.Endpoints;

// ReSharper disable NotAccessedPositionalProperty.Global // Serialized by System.Text.Json as the wire shape, never read in code
internal sealed record SubtitleSearchCandidateDto(
    string CandidateId,
    string Language,
    string ReleaseName,
    int MatchPercent,
    int DownloadCount);

internal sealed record SubtitleQuotaDto(int? RemainingDownloads, System.DateTimeOffset? ResetAtUtc);

internal sealed record SubtitleDownloadAvailabilityDto(bool Enabled, string Reason);

internal sealed record SubtitleSearchResponse(
    SubtitleSearchCandidateDto[] Candidates,
    SubtitleQuotaDto Quota,
    SubtitleDownloadAvailabilityDto DownloadAvailability);
// ReSharper restore NotAccessedPositionalProperty.Global
