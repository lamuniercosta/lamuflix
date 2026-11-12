namespace LamuFlix.Core.Features.Subtitles;

public static class SubtitleProblemReasons
{
    public const string InvalidRequest = "invalid_request";

    public const string StaleCandidate = "stale_candidate";

    public const string AccessDenied = "access_denied";

    public const string MovieNotFound = "movie_not_found";

    public const string ReadOnlyLibrary = "read_only_library";

    public const string ProviderNotConfigured = "provider_not_configured";

    public const string SearchRateLimited = "search_rate_limited";

    public const string DownloadQuotaExhausted = "download_quota_exhausted";

    public const string ProviderUnavailable = "provider_unavailable";

    public const string ProviderTimeout = "provider_timeout";
}
