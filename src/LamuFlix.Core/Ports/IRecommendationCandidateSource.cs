using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface IRecommendationCandidateSource
{
    Task<IReadOnlyList<Movie>> GetCandidatesAsync(MovieId selectedMovieId, CancellationToken ct);
}
