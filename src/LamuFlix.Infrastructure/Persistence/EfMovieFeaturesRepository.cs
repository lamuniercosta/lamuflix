using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Infrastructure.Persistence;

// Phase 2 registration stub. The parameterized atomic read/upsert behavior lands in Phase 3 (T015).
public sealed class EfMovieFeaturesRepository : IMovieFeaturesRepository
{
    public Task<MovieFeaturesSnapshot?> GetByMovieIdAsync(
        MovieId movieId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Movie features reads are implemented in DEV-413 phase 3.");

    public Task UpsertAsync(MovieFeaturesSnapshot features, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Movie features upserts are implemented in DEV-413 phase 3.");
}
