using FluentValidation;
using LamuFlix.Core.Features.Library;

namespace LamuFlix.Infrastructure.Library;

public sealed class RescanMovieCommandValidator : AbstractValidator<RescanMovieCommand>
{
    public RescanMovieCommandValidator()
    {
        RuleFor(command => command.Id).NotNull();
    }
}
