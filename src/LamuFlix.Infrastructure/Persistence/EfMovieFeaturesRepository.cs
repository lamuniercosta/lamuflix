using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfMovieFeaturesRepository(LamuFlixDbContext db) : IMovieFeaturesRepository
{
    public async Task<MovieFeaturesSnapshot?> GetByMovieIdAsync(
        MovieId movieId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = await db.MovieFeatures
            .AsNoTracking()
            .SingleOrDefaultAsync(features => features.MovieId == movieId, cancellationToken);

        if (record is null)
        {
            return null;
        }

        return new MovieFeaturesSnapshot(
            record.MovieId,
            ImmutableArray.CreateRange(record.Vector.ToArray()),
            record.ComputedAt);
    }

    public async Task UpsertAsync(
        MovieFeaturesSnapshot features,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(features);
        ValidateVector(features.Vector);
        cancellationToken.ThrowIfCancellationRequested();

        var vector = new Vector(features.Vector.ToArray());
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO movie_features (movie_id, vector, computed_at)
            VALUES ({features.MovieId.Value}, {vector}, {features.ComputedAt.ToUniversalTime()})
            ON CONFLICT (movie_id) DO UPDATE
            SET vector = EXCLUDED.vector,
                computed_at = EXCLUDED.computed_at
            """,
            cancellationToken);
    }

    private static void ValidateVector(ImmutableArray<float> vector)
    {
        if (vector.IsDefault)
        {
            throw new ArgumentException("Vector must be initialized.", nameof(vector));
        }

        if (vector.Length != EmbeddingDimensions.Value)
        {
            throw new ArgumentException(
                $"Vector must have exactly {EmbeddingDimensions.Value} elements.",
                nameof(vector));
        }

        foreach (var element in vector)
        {
            if (!float.IsFinite(element))
            {
                throw new ArgumentException("Vector elements must be finite.", nameof(vector));
            }
        }
    }
}
