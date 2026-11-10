using System.Collections.Generic;

namespace LamuFlix.Core.Features.Import;

public sealed record ImportLibraryFoldersResult(
    int Imported,
    int Skipped,
    int Retried,
    IReadOnlyList<ImportLibraryFolderFailure> Failures);
