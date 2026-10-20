using System;
using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Core.Domain;

public sealed class GenreWeightsCache
{
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly object _stateLock = new();
    private GenreWeights? _weights;
    private long _generation;

    public async Task<GenreWeights> GetOrLoadAsync(
        Func<CancellationToken, Task<GenreWeights>> load,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(load);
        ct.ThrowIfCancellationRequested();

        await _loadGate.WaitAsync(ct);
        try
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                long observedGeneration;
                lock (_stateLock)
                {
                    if (_weights is not null)
                    {
                        return _weights;
                    }

                    observedGeneration = _generation;
                }

                var loaded = await load(ct);

                lock (_stateLock)
                {
                    if (_generation == observedGeneration)
                    {
                        _weights = loaded;
                        return loaded;
                    }
                }
            }
        }
        finally
        {
            _loadGate.Release();
        }
    }

    public void Invalidate()
    {
        lock (_stateLock)
        {
            _weights = null;
            _generation++;
        }
    }
}
