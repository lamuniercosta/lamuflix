using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GetForYouRecommendationsQueryValidatorTests
{
    [Theory]
    [InlineData(1, 50)]
    [InlineData(5, 50)]
    [InlineData(10, 50)]
    [InlineData(50, 50)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    public async Task Validate_TakeWithinCandidatePool_HasNoFailures(int take, int candidatePoolSize)
    {
        // arrange
        var validator = NewValidator(candidatePoolSize);

        // act
        var result = await validator.ValidateAsync(new GetForYouRecommendationsQuery(take), CancellationToken.None);

        // assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(-1, 50)]
    [InlineData(int.MinValue, 50)]
    [InlineData(51, 50)]
    [InlineData(101, 100)]
    [InlineData(2, 1)]
    public async Task Validate_TakeOutsideCandidatePool_IsInvalid(int take, int candidatePoolSize)
    {
        // arrange
        var validator = NewValidator(candidatePoolSize);

        // act
        var result = await validator.ValidateAsync(new GetForYouRecommendationsQuery(take), CancellationToken.None);

        // assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(failure => failure.PropertyName == nameof(GetForYouRecommendationsQuery.Take));
    }

    [Fact]
    public async Task Validate_DefaultTakeAgainstDefaultPool_IsValid()
    {
        // arrange
        var validator = NewValidator(new RecommendationOptions().CandidatePoolSize);

        // act
        var result = await validator.ValidateAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.IsValid.ShouldBeTrue();
    }

    private static GetForYouRecommendationsQueryValidator NewValidator(int candidatePoolSize) =>
        new(Microsoft.Extensions.Options.Options.Create(new RecommendationOptions
        {
            CandidatePoolSize = candidatePoolSize,
        }));
}
