using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.Core.Ports;

public interface IRecommendationCandidateSource
{
    Task<IReadOnlyList<RecommendationCandidate>> GetCandidatesAsync(MovieId selectedMovieId, CancellationToken ct);
}
