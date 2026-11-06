using FluentValidation;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Feedback;

namespace LamuFlix.Infrastructure.Pipeline;

public sealed class SetMovieReviewCommandValidator : AbstractValidator<SetMovieReviewCommand>
{
    private const string ReviewField = "review";

    public SetMovieReviewCommandValidator()
    {
        RuleFor(command => command.Id).NotNull();
        RuleFor(command => command.Review)
            .Must(BeAKnownReviewName)
            .OverridePropertyName(ReviewField)
            .WithMessage("Review must be 'none', 'liked' or 'disliked'.");
    }

    private static bool BeAKnownReviewName(string? name) => Review.TryParse(name, out _);
}
