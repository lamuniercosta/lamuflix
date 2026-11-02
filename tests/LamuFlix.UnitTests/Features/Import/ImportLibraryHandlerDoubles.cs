using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Import;

internal sealed class DiscoveryDouble : ILibraryFolderDiscovery
{
    private readonly IReadOnlyList<LibraryPath> folders;
    private readonly Func<int, Exception?>? failureAfterYield;
    private int yielded;

    public DiscoveryDouble(IReadOnlyList<LibraryPath> folders, Func<int, Exception?>? failureAfterYield = null)
    {
        this.folders = folders;
        this.failureAfterYield = failureAfterYield;
    }

    public int YieldedCount => Volatile.Read(ref yielded);

    public int YieldedWindow { get; init; } = int.MaxValue;

    public TaskCompletionSource WindowReached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task? BeforeFailure { get; init; }

    public async IAsyncEnumerable<LibraryPath> DiscoverAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var folder in folders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var failure = failureAfterYield?.Invoke(YieldedCount);
            if (failure is not null)
            {
                if (BeforeFailure is not null)
                {
                    await BeforeFailure;
                }

                throw failure;
            }

            var count = Interlocked.Increment(ref yielded);
            if (count >= YieldedWindow)
            {
                WindowReached.TrySetResult();
            }

            yield return folder;
        }

        await Task.CompletedTask;
    }
}

internal sealed class ImporterDouble : IMovieFolderImporter
{
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentQueue<LibraryPath> imported = new();
    private readonly Func<int, Exception?>? failureAtCall;
    private int calls;
    private int inFlight;
    private int maxInFlight;
    private int createdScopes;
    private int disposedScopes;

    public ImporterDouble(
        int expectedConcurrency,
        int blockFromCall = int.MaxValue,
        Func<int, Exception?>? failureAtCall = null)
    {
        ExpectedConcurrency = expectedConcurrency;
        BlockFromCall = blockFromCall;
        this.failureAtCall = failureAtCall;
    }

    private int ExpectedConcurrency { get; }

    private int BlockFromCall { get; }

    public int Calls => Volatile.Read(ref calls);

    public int MaxInFlight => Volatile.Read(ref maxInFlight);

    public int CreatedScopes => Volatile.Read(ref createdScopes);

    public int DisposedScopes => Volatile.Read(ref disposedScopes);

    public IReadOnlyCollection<LibraryPath> Imported => [.. imported];

    public TaskCompletionSource ReachedConcurrency { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource FirstCallEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource SecondCallEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource FirstImportCommitted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Release() => release.TrySetResult();

    public async Task<MovieId> ImportAsync(LibraryPath folder, CancellationToken cancellationToken)
    {
        var call = Interlocked.Increment(ref calls);
        Interlocked.Increment(ref createdScopes);
        var counted = false;
        try
        {
            var active = Interlocked.Increment(ref inFlight);
            counted = true;
            TrackConcurrency(call, active);

            var failure = failureAtCall?.Invoke(call);
            if (failure is not null)
            {
                throw failure;
            }

            if (call >= BlockFromCall)
            {
                await release.Task.WaitAsync(cancellationToken);
            }

            imported.Enqueue(folder);
            FirstImportCommitted.TrySetResult();
            return new MovieId(call);
        }
        finally
        {
            if (counted)
            {
                Interlocked.Decrement(ref inFlight);
            }

            Interlocked.Increment(ref disposedScopes);
        }
    }

    private void TrackConcurrency(int call, int active)
    {
        InterlockedMax(ref maxInFlight, active);
        if (active >= ExpectedConcurrency)
        {
            ReachedConcurrency.TrySetResult();
        }

        FirstCallEntered.TrySetResult();
        if (call >= 2)
        {
            SecondCallEntered.TrySetResult();
        }
    }

    private static void InterlockedMax(ref int target, int candidate)
    {
        var current = Volatile.Read(ref target);
        while (candidate > current)
        {
            var observed = Interlocked.CompareExchange(ref target, candidate, current);
            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }
}
