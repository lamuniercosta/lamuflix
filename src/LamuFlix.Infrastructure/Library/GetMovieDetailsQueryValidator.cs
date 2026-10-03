using FluentValidation;
using LamuFlix.Core.Features.Library;

namespace LamuFlix.Infrastructure.Library;

public sealed class GetMovieDetailsQueryValidator : AbstractValidator<GetMovieDetailsQuery>
{
    public GetMovieDetailsQueryValidator()
    {
        RuleFor(query => query.Id).NotNull();
    }
}
