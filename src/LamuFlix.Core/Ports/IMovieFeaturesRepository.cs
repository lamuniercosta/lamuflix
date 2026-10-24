using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface IMovieFeaturesRepository
{
    Task<MovieFeaturesSnapshot?> GetByMovieIdAsync(MovieId movieId, CancellationToken cancellationToken = default);

    Task UpsertAsync(MovieFeaturesSnapshot features, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the ids of movies that have a nonblank plot and no stored features row. The selection is a
    /// single database-side movies/movie_features anti-join with no per-movie feature lookups, and its
    /// nonblank-plot eligibility matches the feature handler's canonical .NET
    /// <see cref="string.IsNullOrWhiteSpace(string?)"/> / <see cref="char.IsWhiteSpace(char)"/> Unicode semantics.
    /// It deliberately applies no enrichment-status predicate.
    /// </summary>
    Task<IReadOnlyList<MovieId>> FindMovieIdsMissingFeaturesAsync(CancellationToken cancellationToken = default);
}
