using System;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record MovieFeaturesSnapshot
{
    public MovieFeaturesSnapshot(MovieId movieId, ImmutableArray<float> vector, DateTimeOffset computedAt)
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

        MovieId = movieId;
        Vector = vector;
        ComputedAt = computedAt;
    }

    public MovieId MovieId { get; }

    public ImmutableArray<float> Vector { get; }

    public DateTimeOffset ComputedAt { get; }
}
