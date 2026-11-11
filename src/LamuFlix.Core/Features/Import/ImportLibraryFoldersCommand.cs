using System.Collections.Immutable;

namespace LamuFlix.Core.Features.Import;

public sealed record ImportLibraryFoldersCommand
{
    public ImportLibraryFoldersCommand(ImmutableArray<string> Folders, bool RetryFailed)
    {
        this.Folders = Folders.IsDefault ? [] : Folders;
        this.RetryFailed = RetryFailed;
    }

    public ImmutableArray<string> Folders { get; }

    public bool RetryFailed { get; }
}
