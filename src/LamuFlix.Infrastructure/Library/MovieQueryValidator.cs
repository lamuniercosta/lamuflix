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
        RuleFor(query => query.Runtime).Must(IsValidRuntime);
        RuleFor(query => query.Year).Must(IsValidYear);
    }

    private static bool IsValidRuntime(RuntimeRange? runtime)
    {
        if (runtime is null || runtime.Min is null || runtime.Max is null)
        {
            return true;
        }

        var min = runtime.Min;
        var max = runtime.Max;
        return min <= max;
    }

    private static bool IsValidYear(YearRange? year)
    {
        if (year is null || year.Min is null || year.Max is null)
        {
            return true;
        }

        var min = year.Min;
        var max = year.Max;
        return min <= max;
    }
}
