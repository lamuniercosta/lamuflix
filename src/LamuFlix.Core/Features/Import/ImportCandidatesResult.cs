using System.Collections.Generic;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Import;

public sealed record ImportCandidatesResult(IReadOnlyList<LibraryPath> NewFolders);
