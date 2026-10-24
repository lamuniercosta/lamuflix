using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.Core.Ports;

public interface IRecommendationProfileSource
{
    Task<RecommendationProfileInputs> GetProfileInputsAsync(
        IReadOnlyCollection<MovieId> candidateMovieIds,
        CancellationToken ct);

    Task<TasteProfile> GetTasteProfileAsync(CancellationToken ct);
}
