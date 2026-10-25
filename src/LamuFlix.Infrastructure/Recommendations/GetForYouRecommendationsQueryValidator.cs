using System;
using FluentValidation;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Recommendations;

public sealed class GetForYouRecommendationsQueryValidator : AbstractValidator<GetForYouRecommendationsQuery>
{
    public GetForYouRecommendationsQueryValidator(IOptions<RecommendationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var candidatePoolSize = options.Value.CandidatePoolSize;

        RuleFor(query => query.Take).GreaterThan(0);
        RuleFor(query => query.Take).LessThanOrEqualTo(candidatePoolSize);
    }
}
