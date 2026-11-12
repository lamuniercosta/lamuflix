using FluentValidation;
using LamuFlix.Core.Features.Subtitles;

namespace LamuFlix.Infrastructure.Pipeline;

public sealed class DownloadSubtitleCommandValidator : AbstractValidator<DownloadSubtitleCommand>
{
    public DownloadSubtitleCommandValidator()
    {
        RuleFor(command => command.Id).NotNull();
        RuleFor(command => command.CandidateId).NotEmpty();
    }
}
