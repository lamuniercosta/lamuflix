using System.Collections.Generic;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Library;

public sealed record RescanLibraryResult(
    int Changed,
    int Unchanged,
    int Failed,
    IReadOnlyList<MovieId> FailedMovieIds);
