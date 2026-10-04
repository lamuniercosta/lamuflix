using FluentValidation;
using LamuFlix.Core.Features.Enrichment;

namespace LamuFlix.Infrastructure.Pipeline;

public sealed class ProcessEnrichmentCommandValidator : AbstractValidator<ProcessEnrichmentCommand>
{
    private const int AttemptFloor = 1;

    public ProcessEnrichmentCommandValidator()
    {
        RuleFor(command => command.MovieId).NotNull();
        RuleFor(command => command.Attempt).GreaterThanOrEqualTo(AttemptFloor);
    }
}