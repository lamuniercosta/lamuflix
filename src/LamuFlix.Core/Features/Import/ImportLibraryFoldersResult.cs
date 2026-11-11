using System.Collections.Immutable;

namespace LamuFlix.Core.Features.Import;

public sealed record ImportLibraryFoldersResult(
    int Imported,
    int Skipped,
    int Retried,
    ImmutableArray<ImportLibraryFolderFailure> Failures);
