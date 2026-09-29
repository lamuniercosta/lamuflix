using FluentValidation;
using LamuFlix.Core.Library;

namespace LamuFlix.Infrastructure.Library;

public sealed class MovieQueryValidator : AbstractValidator<MovieQuery>
{
    public MovieQueryValidator()
    {
        RuleFor(query => query.Page.Number).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Page.Size).InclusiveBetween(1, 100);
        RuleFor(query => query.Sort).NotNull();
        RuleFor(query => query.Direction).NotNull();
        RuleFor(query => query.Runtime!.Min)
            .LessThanOrEqualTo(query => query.Runtime!.Max)
            .When(query => query.Runtime is { Min: not null, Max: not null });
        RuleFor(query => query.Year!.Min)
            .LessThanOrEqualTo(query => query.Year!.Max)
            .When(query => query.Year is { Min: not null, Max: not null });
    }
}
