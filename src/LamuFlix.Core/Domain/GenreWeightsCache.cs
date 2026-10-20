using System;
using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Core.Domain;

public sealed class GenreWeightsCache
{
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly object _stateLock = new();
    private GenreWeights? _weights;
    private object _stateVersion = new();

    public async Task<GenreWeights> GetOrLoadAsync(
        Func<CancellationToken, Task<GenreWeights>> load,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(load);

        await _loadGate.WaitAsync(ct);
        try
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                object observedVersion;
                lock (_stateLock)
                {
                    if (_weights is not null)
                    {
                        return _weights;
                    }

                    observedVersion = _stateVersion;
                }

                var loaded = await load(ct);

                lock (_stateLock)
                {
                    if (ReferenceEquals(_stateVersion, observedVersion))
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
            _stateVersion = new object();
        }
    }
}
