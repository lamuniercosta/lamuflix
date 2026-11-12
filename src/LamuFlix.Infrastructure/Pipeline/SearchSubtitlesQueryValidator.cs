using FluentValidation;
using LamuFlix.Core.Features.Subtitles;

namespace LamuFlix.Infrastructure.Pipeline;

public sealed class SearchSubtitlesQueryValidator : AbstractValidator<SearchSubtitlesQuery>
{
    public SearchSubtitlesQueryValidator()
    {
        RuleFor(query => query.Id).NotNull();
        RuleFor(query => query.Language).NotEmpty();
    }
}
