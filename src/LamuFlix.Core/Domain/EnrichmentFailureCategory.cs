using System.Diagnostics;

namespace LamuFlix.Core.Domain;

public sealed record EnrichmentFailureCategory
{
    // Codes are const so no literal lives in a static initializer: Stryker does not mutate const
    // declarations, and a mutant captured by a static readonly initializer would be frozen for the
    // whole test-server process (see DEV-410). Descriptions and retry flags are computed per call.
    private const string ProviderUnavailableCode = "provider_unavailable";
    private const string RateLimitedCode = "rate_limited";
    private const string InvalidResponseCode = "invalid_response";
    private const string UnknownCode = "unknown";
    private const string DuplicateImdbIdCode = "duplicate_imdb_id";

    public static readonly EnrichmentFailureCategory ProviderUnavailable = new(ProviderUnavailableCode);

    public static readonly EnrichmentFailureCategory RateLimited = new(RateLimitedCode);

    public static readonly EnrichmentFailureCategory InvalidResponse = new(InvalidResponseCode);

    public static readonly EnrichmentFailureCategory Unknown = new(UnknownCode);

    public static readonly EnrichmentFailureCategory DuplicateImdbId = new(DuplicateImdbIdCode);

    // Expression-bodied so Stryker has no constructor block to remove: that mutant, active when the
    // statics above initialise, would leave Code null for every later mutant in the process.
    private EnrichmentFailureCategory(string code) => Code = code;

    public string Code { get; }

    // Caller-safe contract member (ADR-0006, DEV-295); consumed by tests until an API surface reads it.
    // ReSharper disable once UnusedMember.Global
    public string SafeDescription => Code switch
    {
        ProviderUnavailableCode => "The metadata provider is temporarily unavailable.",
        RateLimitedCode => "The metadata provider is temporarily rate limiting requests.",
        InvalidResponseCode => "The metadata provider returned an unusable response.",
        UnknownCode => "The enrichment failed for an unknown reason.",
        DuplicateImdbIdCode => "Another movie in the library already has this IMDb id.",
        _ => throw new UnreachableException(),
    };

    public bool IsRetryable => Code switch
    {
        ProviderUnavailableCode => true,
        RateLimitedCode => true,
        InvalidResponseCode => false,
        UnknownCode => true,
        DuplicateImdbIdCode => false,
        _ => throw new UnreachableException(),
    };
}
