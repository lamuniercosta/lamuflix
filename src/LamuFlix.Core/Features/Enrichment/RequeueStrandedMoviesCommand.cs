using System.Collections.Generic;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Enrichment;

public sealed record RequeueStrandedMoviesCommand(IReadOnlyList<MovieId> MovieIds);
