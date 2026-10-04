using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class EnrichmentFailureCategoryTests
{
    [Fact]
    public void ProviderUnavailable_SafeDescription_IsPreserved()
    {
        EnrichmentFailureCategory.ProviderUnavailable.SafeDescription.ShouldBe(
            "The metadata provider is temporarily unavailable.");
    }

    [Fact]
    public void RateLimited_SafeDescription_IsPreserved()
    {
        EnrichmentFailureCategory.RateLimited.SafeDescription.ShouldBe(
            "The metadata provider is temporarily rate limiting requests.");
    }

    [Fact]
    public void InvalidResponse_SafeDescription_IsPreserved()
    {
        EnrichmentFailureCategory.InvalidResponse.SafeDescription.ShouldBe(
            "The metadata provider returned an unusable response.");
    }
}