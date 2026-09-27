namespace LamuFlix.Core.Domain;

public sealed record EnrichmentFailureCategory
{
    public static readonly EnrichmentFailureCategory ProviderUnavailable = new(
        "provider_unavailable",
        "The metadata provider is temporarily unavailable.",
        true);

    public static readonly EnrichmentFailureCategory RateLimited = new(
        "rate_limited",
        "The metadata provider is temporarily rate limiting requests.",
        true);

    public static readonly EnrichmentFailureCategory InvalidResponse = new(
        "invalid_response",
        "The metadata provider returned an unusable response.",
        false);

    public static readonly EnrichmentFailureCategory Unknown = new(
        "unknown",
        "The enrichment failed for an unknown reason.",
        true);

    private EnrichmentFailureCategory(string code, string safeDescription, bool isRetryable)
    {
        Code = code;
        SafeDescription = safeDescription;
        IsRetryable = isRetryable;
    }

    public string Code { get; }

    public string SafeDescription { get; }

    public bool IsRetryable { get; }
}
