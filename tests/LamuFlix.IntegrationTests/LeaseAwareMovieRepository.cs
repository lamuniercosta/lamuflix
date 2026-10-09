using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.IntegrationTests;

/// Implements DEV-301 FR-002: a claim succeeds only for a Pending movie whose last attempt has aged
/// past the lease, and a successful claim stamps LastAttemptAt and increments the attempt counter.
public sealed class LeaseAwareMovieRepository(TimeProvider time, TimeSpan claimLease) : IMovieRepository
{
    private readonly Dictionary<MovieId, Movie> movies = [];

    public void Add(Movie movie) => movies[movie.Id] = movie;

    public Movie? Find(MovieId id) => movies.GetValueOrDefault(id);

    public async Task<Movie?> GetAsync(MovieId id, CancellationToken ct) =>
        await Task.FromResult(movies.GetValueOrDefault(id));

    public Task AddAsync(Movie movie, CancellationToken ct)
    {
        movies[movie.Id] = movie;
        return Task.CompletedTask;
    }

    public Task<MovieId> NextIdentityAsync(CancellationToken ct) =>
        Task.FromResult(new MovieId(movies.Count + 1));

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

    public Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct)
    {
        if (!movies.TryGetValue(id, out var movie))
        {
            return Task.FromResult(false);
        }

        if (movie.Status != EnrichmentStatus.Pending || !LeaseExpired(movie))
        {
            return Task.FromResult(false);
        }

        var now = time.GetUtcNow();
        movies[id] = Movie.Rehydrate(
            movie.Id,
            movie.Title,
            movie.Path,
            movie.Format,
            movie.IsInWatchlist,
            movie.Metadata,
            movie.Status,
            movie.EnrichedAt,
            movie.EnrichmentAttempts + 1,
            movie.LastFailureCategory,
            now);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<MovieId>> FindStrandedMovieIdsAsync(DateTimeOffset leaseCutoff, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<MovieId>>([.. movies.Values
            .Where(movie => movie.Status == EnrichmentStatus.Pending
                && (movie.LastAttemptAt is null || movie.LastAttemptAt < leaseCutoff))
            .Select(movie => movie.Id)]);

    private bool LeaseExpired(Movie movie) =>
        movie.LastAttemptAt is null || movie.LastAttemptAt < time.GetUtcNow() - claimLease;
}
