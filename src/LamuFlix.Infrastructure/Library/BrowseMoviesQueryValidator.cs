using FluentValidation;
using LamuFlix.Core.Features.Library;

namespace LamuFlix.Infrastructure.Library;

public sealed class BrowseMoviesQueryValidator : AbstractValidator<BrowseMoviesQuery>
{
    public BrowseMoviesQueryValidator()
    {
        RuleFor(query => query.Query).SetValidator(new MovieQueryValidator());
    }
}
