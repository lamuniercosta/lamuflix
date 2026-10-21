using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Infrastructure.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GetRecommendationsQueryValidatorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(int.MaxValue)]
    public async Task Validate_PositiveTake_HasNoFailures(int take)
    {
        // arrange
        var validator = new GetRecommendationsQueryValidator();

        // act
        var result = await validator.ValidateAsync(new GetRecommendationsQuery(new MovieId(7), take), CancellationToken.None);

        // assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task Validate_NonPositiveTake_IsInvalid(int take)
    {
        // arrange
        var validator = new GetRecommendationsQueryValidator();

        // act
        var result = await validator.ValidateAsync(new GetRecommendationsQuery(new MovieId(7), take), CancellationToken.None);

        // assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(failure => failure.PropertyName == nameof(GetRecommendationsQuery.Take));
    }
}
