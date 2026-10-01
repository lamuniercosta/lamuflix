using System.Collections.Generic;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class EnrichmentRetryPolicyTests
{
    private const int MaxAttempts = 3;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Decide_RetryableCategoryBelowLimit_ReturnsRetry(int attempt)
    {
        // arrange
        var category = EnrichmentFailureCategory.ProviderUnavailable;

        // act
        var decision = EnrichmentRetryPolicy.Decide(category, attempt, MaxAttempts);

        // assert
        decision.ShouldNotBeNull();
        decision.Action.ShouldBe(EnrichmentFailureAction.Retry);
        decision.NextAttempt.ShouldBe(attempt + 1);
    }

    [Fact]
    public void Decide_RateLimitedCategory_ReturnsRetryDelayed()
    {
        // arrange
        var category = EnrichmentFailureCategory.RateLimited;

        // act
        var decision = EnrichmentRetryPolicy.Decide(category, attempt: 1, MaxAttempts);

        // assert
        decision.ShouldNotBeNull();
        decision.Action.ShouldBe(EnrichmentFailureAction.RetryDelayed);
        decision.NextAttempt.ShouldBe(2);
    }

    [Fact]
    public void Decide_NonRetryableCategory_ReturnsNull()
    {
        // arrange
        var category = EnrichmentFailureCategory.InvalidResponse;

        // act
        var decision = EnrichmentRetryPolicy.Decide(category, attempt: 1, MaxAttempts);

        // assert
        decision.ShouldBeNull();
    }

    [Fact]
    public void Decide_RetryableCategoryAtMaxAttempts_ReturnsNull()
    {
        // arrange
        var category = EnrichmentFailureCategory.ProviderUnavailable;

        // act
        var decision = EnrichmentRetryPolicy.Decide(category, MaxAttempts, MaxAttempts);

        // assert
        decision.ShouldBeNull();
    }

    [Fact]
    public void Decide_RetryableCategoryPastMaxAttempts_ReturnsNull()
    {
        // arrange
        var category = EnrichmentFailureCategory.ProviderUnavailable;

        // act
        var decision = EnrichmentRetryPolicy.Decide(category, MaxAttempts + 1, MaxAttempts);

        // assert
        decision.ShouldBeNull();
    }

    [Fact]
    public void Decide_EveryCategoryAtEveryAttempt_MatchesTheRetryableAndLimitRule()
    {
        // arrange
        var cases = new List<(EnrichmentFailureCategory Category, int Attempt, bool ExpectedRetry)>();

        // act
        foreach (var category in Categories())
        {
            foreach (var attempt in new[] { 1, 2, 3, 4 })
            {
                cases.Add((category, attempt, category.IsRetryable && attempt < MaxAttempts));
            }
        }

        // assert
        foreach (var (category, attempt, expectedRetry) in cases)
        {
            var decision = EnrichmentRetryPolicy.Decide(category, attempt, MaxAttempts);
            if (expectedRetry)
            {
                decision.ShouldNotBeNull();
                decision.NextAttempt.ShouldBe(attempt + 1);
            }
            else
            {
                decision.ShouldBeNull();
            }
        }
    }

    private static IEnumerable<EnrichmentFailureCategory> Categories() =>
    [
        EnrichmentFailureCategory.ProviderUnavailable,
        EnrichmentFailureCategory.RateLimited,
        EnrichmentFailureCategory.InvalidResponse,
        EnrichmentFailureCategory.Unknown,
    ];
}