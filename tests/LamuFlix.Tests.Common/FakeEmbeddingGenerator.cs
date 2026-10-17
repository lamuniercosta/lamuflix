using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.AI;

namespace LamuFlix.Tests.Common;

public sealed class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private const int CounterCount = 48;
    private const int WordsPerHash = 8;
    private const double WordScale = 2147483648.0;

    private readonly EmbeddingGeneratorMetadata metadata;

    public FakeEmbeddingGenerator(int? reportedDimensions = EmbeddingDimensions.Value)
    {
        metadata = new EmbeddingGeneratorMetadata(defaultModelDimensions: reportedDimensions);
    }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (options?.Dimensions is { } dimensions)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(dimensions, EmbeddingDimensions.Value);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var embeddings = new List<Embedding<float>>();
        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(value);
            embeddings.Add(new Embedding<float>(CreateVector(value)));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        if (serviceKey is not null)
        {
            return null;
        }

        if (serviceType == typeof(EmbeddingGeneratorMetadata))
        {
            return metadata;
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
        // The fake owns no resources, so an empty body is the intended no-op.
    }

    private static float[] CreateVector(string value)
    {
        var textBytes = Encoding.UTF8.GetBytes(value);
        var block = new byte[textBytes.Length + sizeof(uint)];
        textBytes.CopyTo(block, 0);

        var raw = new double[EmbeddingDimensions.Value];
        var sumOfSquares = 0.0;
        var index = 0;

        for (uint counter = 0; counter < CounterCount; counter++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(block.AsSpan(textBytes.Length), counter);
            var hash = SHA256.HashData(block);
            for (var wordIndex = 0; wordIndex < WordsPerHash; wordIndex++)
            {
                var word = BinaryPrimitives.ReadUInt32BigEndian(hash.AsSpan(wordIndex * sizeof(uint)));
                var mapped = (word / WordScale) - 1.0;
                raw[index++] = mapped;
                sumOfSquares += mapped * mapped;
            }
        }

        var vector = new float[EmbeddingDimensions.Value];
        var norm = Math.Sqrt(sumOfSquares);
        if (norm == 0)
        {
            vector[0] = 1f;
            return vector;
        }

        for (var i = 0; i < raw.Length; i++)
        {
            vector[i] = (float)(raw[i] / norm);
        }

        return vector;
    }
}
