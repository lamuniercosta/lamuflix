using FluentValidation;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.Infrastructure.Recommendations;

public sealed class GetRecommendationsQueryValidator : AbstractValidator<GetRecommendationsQuery>
{
    public GetRecommendationsQueryValidator()
    {
        RuleFor(query => query.Take).GreaterThan(0);
    }
}
