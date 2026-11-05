using FluentValidation;
using LamuFlix.Core.Features.Library;

namespace LamuFlix.Infrastructure.Library;

public sealed class RescanLibraryCommandValidator : AbstractValidator<RescanLibraryCommand>;
