using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Domain;

public sealed class KeywordWeightsCache
{
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly object _stateLock = new();
    private KeywordCounts? _counts;
    private object _stateVersion = new();

    public async Task<KeywordCounts> GetOrLoadAsync(
        Func<CancellationToken, Task<KeywordCounts>> load,
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
                    if (_counts is not null)
                    {
                        return _counts;
                    }

                    observedVersion = _stateVersion;
                }

                var loaded = await load(ct);

                lock (_stateLock)
                {
                    if (ReferenceEquals(_stateVersion, observedVersion))
                    {
                        _counts = loaded;
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
            _counts = null;
            _stateVersion = new object();
        }
    }
}