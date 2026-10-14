using System;
using LamuFlix.Core.Features.Enrichment;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class EnrichmentRetryPolicySurvivorTests
{
    [Fact]
    public void Decide_NullCategory_ThrowsArgumentNullExceptionNamingCategory()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var thrown = Should.Throw<ArgumentNullException>(() => EnrichmentRetryPolicy.Decide(null!, 1, 3));

        // assert
        thrown.ParamName.ShouldBe("category");
    }
}
