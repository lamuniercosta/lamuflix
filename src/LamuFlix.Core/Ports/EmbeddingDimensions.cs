using System;
using Microsoft.Extensions.AI;

namespace LamuFlix.Core.Ports;

public static class EmbeddingDimensions
{
    public const int Value = 384;

    public static void Validate(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        ArgumentNullException.ThrowIfNull(generator);

        var metadata = (EmbeddingGeneratorMetadata?)generator.GetService(typeof(EmbeddingGeneratorMetadata));
        if (metadata?.DefaultModelDimensions != Value)
        {
            throw new InvalidOperationException(
                $"The embedding generator must report a default model dimension of {Value}.");
        }
    }
}
