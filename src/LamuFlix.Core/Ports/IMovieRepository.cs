using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface IMovieRepository
{
    Task<Movie?> GetAsync(MovieId id, CancellationToken ct);

    Task AddAsync(Movie movie, CancellationToken ct);

    Task<MovieId> NextIdentityAsync(CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// An atomic claim attempt. Returns true only when the movie exists and this call newly claims it,
    /// and false when the movie is absent or already claimed.
    /// </summary>
    Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct);
}
