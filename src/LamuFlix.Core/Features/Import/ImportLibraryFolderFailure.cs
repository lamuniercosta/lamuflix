using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Import;

public sealed record ImportLibraryFolderFailure(LibraryPath Folder, string Reason);
