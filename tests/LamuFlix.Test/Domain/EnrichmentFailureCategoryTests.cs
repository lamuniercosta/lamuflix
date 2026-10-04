using System;
using System.Reflection;
using LamuFlix.Core.Domain;
using Shouldly;
using Xunit;

namespace LamuFlix.Test.Domain;

public class EnrichmentFailureCategoryTests
{
    public static TheoryData<EnrichmentFailureCategory, string, string, bool> FrozenInstances { get; } = new()
    {
        {
            EnrichmentFailureCategory.ProviderUnavailable,
            "provider_unavailable",
            "The metadata provider is temporarily unavailable.",
            true
        },
        {
            EnrichmentFailureCategory.RateLimited,
            "rate_limited",
            "The metadata provider is temporarily rate limiting requests.",
            true
        },
        {
            EnrichmentFailureCategory.InvalidResponse,
            "invalid_response",
            "The metadata provider returned an unusable response.",
            false
        },
        {
            EnrichmentFailureCategory.Unknown,
            "unknown",
            "The enrichment failed for an unknown reason.",
            true
        },
    };

    [Theory]
    [MemberData(nameof(FrozenInstances))]
    public void Instance_FrozenContract_MatchesCodeDescriptionAndRetryFlag(
        EnrichmentFailureCategory category,
        string code,
        string safeDescription,
        bool isRetryable)
    {
        // arrange
        // act
        // assert
        category.Code.ShouldBe(code);
        category.SafeDescription.ShouldBe(safeDescription);
        category.IsRetryable.ShouldBe(isRetryable);
        category.SafeDescription.ShouldNotBeNullOrWhiteSpace();
        category.SafeDescription.Contains("://", StringComparison.Ordinal).ShouldBeFalse();
        category.SafeDescription.Contains("Exception", StringComparison.Ordinal).ShouldBeFalse();
        category.SafeDescription.Contains("localhost", StringComparison.Ordinal).ShouldBeFalse();
    }

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

    [Fact]
    public void Instances_SameProperties_AreValueEqual()
    {
        // arrange
        var duplicate = Activator.CreateInstance(
            typeof(EnrichmentFailureCategory),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: ["provider_unavailable", "The metadata provider is temporarily unavailable.", true],
            culture: null);

        // act
        var created = duplicate.ShouldBeOfType<EnrichmentFailureCategory>();

        // assert
        created.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        created.ShouldNotBe(EnrichmentFailureCategory.RateLimited);
        EnrichmentFailureCategory.ProviderUnavailable.ShouldNotBe(EnrichmentFailureCategory.Unknown);
        EnrichmentFailureCategory.RateLimited.ShouldNotBe(EnrichmentFailureCategory.InvalidResponse);
    }
}
