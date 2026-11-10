using System.Collections.Generic;

namespace LamuFlix.Core.Features.Import;

public sealed record ImportLibraryFoldersCommand(IReadOnlyList<string> Folders, bool RetryFailed);
