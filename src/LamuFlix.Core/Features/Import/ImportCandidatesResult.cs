using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Import;

public sealed record ImportCandidatesResult(ImmutableArray<LibraryPath> NewFolders);
