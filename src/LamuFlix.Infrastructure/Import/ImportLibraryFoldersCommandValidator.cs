using FluentValidation;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;

namespace LamuFlix.Infrastructure.Import;

public sealed class ImportLibraryFoldersCommandValidator : AbstractValidator<ImportLibraryFoldersCommand>
{
    public ImportLibraryFoldersCommandValidator()
    {
        RuleFor(command => command.Folders)
            .Must((command, folders) => command.RetryFailed || folders.Length > 0)
            .WithMessage("Select at least one folder or retry failed enrichments.");

        RuleForEach(command => command.Folders)
            .Must(folder => LibraryPath.TryCreate(folder, out _))
            .WithMessage("Folder path must be non-blank and must not contain '..'.");
    }
}
