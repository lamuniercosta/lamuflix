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
}