using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class EnrichmentFailureCategoryTests
{
    public static TheoryData<EnrichmentFailureCategory, string> SafeDescriptions() =>
    [
        (EnrichmentFailureCategory.ProviderUnavailable, "The metadata provider is temporarily unavailable."),
        (EnrichmentFailureCategory.RateLimited, "The metadata provider is temporarily rate limiting requests."),
        (EnrichmentFailureCategory.InvalidResponse, "The metadata provider returned an unusable response."),
        (EnrichmentFailureCategory.Unknown, "The enrichment failed for an unknown reason."),
        (EnrichmentFailureCategory.DuplicateImdbId, "Another movie in the library already has this IMDb id."),
        (EnrichmentFailureCategory.QuotaExhausted, "Local OMDb daily request quota admission was denied."),
    ];

    public static TheoryData<EnrichmentFailureCategory, string> Codes() =>
    [
        (EnrichmentFailureCategory.ProviderUnavailable, "provider_unavailable"),
        (EnrichmentFailureCategory.RateLimited, "rate_limited"),
        (EnrichmentFailureCategory.InvalidResponse, "invalid_response"),
        (EnrichmentFailureCategory.Unknown, "unknown"),
        (EnrichmentFailureCategory.DuplicateImdbId, "duplicate_imdb_id"),
        (EnrichmentFailureCategory.QuotaExhausted, "quota_exhausted"),
    ];

    public static TheoryData<EnrichmentFailureCategory, bool> Retryability() =>
    [
        (EnrichmentFailureCategory.ProviderUnavailable, true),
        (EnrichmentFailureCategory.RateLimited, true),
        (EnrichmentFailureCategory.InvalidResponse, false),
        (EnrichmentFailureCategory.Unknown, true),
        (EnrichmentFailureCategory.DuplicateImdbId, false),
        (EnrichmentFailureCategory.QuotaExhausted, true),
    ];

    [Theory]
    [MemberData(nameof(SafeDescriptions))]
    public void SafeDescription_IsCallerSafe(
        EnrichmentFailureCategory category,
        string expectedDescription)
    {
        category.SafeDescription.ShouldBe(expectedDescription);
        category.SafeDescription.ShouldNotBeNullOrWhiteSpace();
        category.SafeDescription.Contains("://", StringComparison.Ordinal).ShouldBeFalse();
        category.SafeDescription.Contains("Exception", StringComparison.Ordinal).ShouldBeFalse();
        category.SafeDescription.Contains("localhost", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(Codes))]
    public void Code_KnownCategory_IsStable(
        EnrichmentFailureCategory category,
        string expectedCode)
    {
        category.Code.ShouldBe(expectedCode);
        category.Code.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [MemberData(nameof(Retryability))]
    public void IsRetryable_KnownCategory_MatchesCategorySemantics(
        EnrichmentFailureCategory category,
        bool expectedRetryability) =>
        category.IsRetryable.ShouldBe(expectedRetryability);
}
