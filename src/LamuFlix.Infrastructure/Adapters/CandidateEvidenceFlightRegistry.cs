using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.Infrastructure.Adapters;

internal sealed class CandidateEvidenceFlightRegistry
{
    private readonly ConcurrentDictionary<int, Task<CandidateImdbEvidence?>> flights = new();

    public async ValueTask<CandidateImdbEvidence?> RunAsync(
        int tmdbId,
        Func<CancellationToken, ValueTask<CandidateImdbEvidence?>> operation,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var completion = new TaskCompletionSource<CandidateImdbEvidence?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var flight = flights.GetOrAdd(tmdbId, _ => completion.Task);
        if (ReferenceEquals(flight, completion.Task))
        {
            await LeadAsync(tmdbId, completion, operation, ct);
        }

        try
        {
            return await flight.WaitAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task LeadAsync(
        int tmdbId,
        TaskCompletionSource<CandidateImdbEvidence?> completion,
        Func<CancellationToken, ValueTask<CandidateImdbEvidence?>> operation,
        CancellationToken ct)
    {
        try
        {
            var outcome = await operation(ct);
            completion.TrySetResult(outcome);
        }
        catch (OperationCanceledException)
        {
            completion.TrySetCanceled();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            flights.TryRemove(new KeyValuePair<int, Task<CandidateImdbEvidence?>>(tmdbId, completion.Task));
        }
    }
}
