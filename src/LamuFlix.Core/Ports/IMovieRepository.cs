using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface IMovieRepository
{
    Task<Movie?> GetAsync(MovieId id, CancellationToken ct);

    Task AddAsync(Movie movie, CancellationToken ct);

    /// <summary>
    /// An atomic removal attempt. Returns true only when the movie row was removed, and false when the
    /// movie is absent. The movie's watchlist and metadata columns leave with the row, and its rows in
    /// movie_actors, movie_directors, and movie_genres leave through the existing database cascades.
    /// Never touches the physical media file.
    /// </summary>
    Task<bool> TryDeleteAsync(MovieId id, CancellationToken ct);

    Task<MovieId> NextIdentityAsync(CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// An atomic claim attempt. Returns true only when the movie exists and this call newly claims it,
    /// and false when the movie is absent or already claimed.
    /// </summary>
    Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct);

    Task<IReadOnlyList<MovieId>> FindStrandedMovieIdsAsync(DateTimeOffset leaseCutoff, CancellationToken ct);

    Task<string?> GetPlotAsync(MovieId id, CancellationToken ct);

    /// <summary>
    /// Returns the id of the movie whose resolved metadata holds this IMDb id (the uniquely indexed imdb_id
    /// column), or null when no movie holds it.
    /// </summary>
    Task<MovieId?> FindMovieIdByImdbIdAsync(ImdbId imdbId, CancellationToken ct);
}
