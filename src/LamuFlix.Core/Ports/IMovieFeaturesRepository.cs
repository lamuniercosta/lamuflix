using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface IMovieFeaturesRepository
{
    Task<MovieFeaturesSnapshot?> GetByMovieIdAsync(MovieId movieId, CancellationToken cancellationToken = default);

    Task UpsertAsync(MovieFeaturesSnapshot features, CancellationToken cancellationToken = default);
}
