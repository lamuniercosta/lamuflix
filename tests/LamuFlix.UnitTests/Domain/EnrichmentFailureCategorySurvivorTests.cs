using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class EnrichmentFailureCategorySurvivorTests
{
    public static TheoryData<EnrichmentFailureCategory, bool> RetryableFlags() =>
    [
        (EnrichmentFailureCategory.ProviderUnavailable, true),
        (EnrichmentFailureCategory.RateLimited, true),
        (EnrichmentFailureCategory.InvalidResponse, false),
        (EnrichmentFailureCategory.Unknown, true),
        (EnrichmentFailureCategory.DuplicateImdbId, false),
    ];

    [Theory]
    [MemberData(nameof(RetryableFlags))]
    public void IsRetryable_EachCategory_MatchesTheRetryContract(
        EnrichmentFailureCategory category,
        bool expectedRetryable)
    {
        // arrange
        // act
        var retryable = category.IsRetryable;

        // assert
        retryable.ShouldBe(expectedRetryable);
    }
}
