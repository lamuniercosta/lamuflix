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
        RuleFor(query => query.Runtime!).Must(RuntimeIsOrdered).When(query => query.Runtime is not null);
        RuleFor(query => query.Year!).Must(YearIsOrdered).When(query => query.Year is not null);
    }

    private static bool RuntimeIsOrdered(RuntimeRange range) =>
        range.Min is null || range.Max is null || range.Min <= range.Max;

    private static bool YearIsOrdered(YearRange range) =>
        range.Min is null || range.Max is null || range.Min <= range.Max;
}
