using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class RecordingEmbeddingGenerator(
    Func<IReadOnlyList<string>, CancellationToken, Task<GeneratedEmbeddings<Embedding<float>>>> generate)
    : IEmbeddingGenerator<string, Embedding<float>>
{
    public int CallCount { get; private set; }

    public IReadOnlyList<string> LastInputs { get; private set; } = [];

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        _ = options;
        CallCount++;
        LastInputs = [.. values];
        cancellationToken.ThrowIfCancellationRequested();
        return await generate(LastInputs, cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        _ = serviceKey;
        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
        // The recording wrapper owns no resources.
    }
}
