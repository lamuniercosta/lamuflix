using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Domain;

public sealed class GenreWeightsCacheTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task GetOrLoadAsync_NullLoader_ThrowsArgumentNullException()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var ct = TestContext.Current.CancellationToken;

        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        await Should.ThrowAsync<ArgumentNullException>(() => cache.GetOrLoadAsync(null!, ct));
    }

    [Fact]
    public async Task GetOrLoadAsync_ColdCache_LoadsOnceAndReusesSnapshot()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var expected = Weights(1);
        var loads = 0;
        var ct = TestContext.Current.CancellationToken;

        // act
        var loaded = await cache.GetOrLoadAsync(
            _ =>
            {
                Interlocked.Increment(ref loads);
                return Task.FromResult(expected);
            },
            ct);
        var cached = await cache.GetOrLoadAsync(
            _ => throw new InvalidOperationException("The loader must not run for a cache hit."),
            ct);

        // assert
        loaded.ShouldBeSameAs(expected);
        cached.ShouldBeSameAs(expected);
        loads.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrLoadAsync_ConcurrentCallers_LoadsOnceAndSharesSnapshot()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var expected = Weights(2);
        var entered = NewSignal();
        var release = NewSignal();
        var loads = 0;
        var ct = TestContext.Current.CancellationToken;

        async Task<GenreWeights> Load(CancellationToken token)
        {
            Interlocked.Increment(ref loads);
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            return expected;
        }

        // act
        var first = cache.GetOrLoadAsync(Load, ct);
        await entered.Task.WaitAsync(Timeout, ct);
        var second = cache.GetOrLoadAsync(Load, ct);
        release.SetResult();
        var results = await Task.WhenAll(first, second).WaitAsync(Timeout, ct);

        // assert
        results[0].ShouldBeSameAs(expected);
        results[1].ShouldBeSameAs(expected);
        loads.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrLoadAsync_InvalidatedDuringLoad_DiscardsStaleResultAndRetries()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var stale = Weights(1);
        var fresh = Weights(3);
        var entered = NewSignal();
        var release = NewSignal();
        var loads = 0;
        var ct = TestContext.Current.CancellationToken;

        async Task<GenreWeights> Load(CancellationToken token)
        {
            if (Interlocked.Increment(ref loads) == 1)
            {
                entered.TrySetResult();
                await release.Task.WaitAsync(token);
                return stale;
            }

            return fresh;
        }

        var inFlight = cache.GetOrLoadAsync(Load, ct);
        await entered.Task.WaitAsync(Timeout, ct);

        // act
        cache.Invalidate();
        release.SetResult();
        var result = await inFlight.WaitAsync(Timeout, ct);
        var cached = await cache.GetOrLoadAsync(
            _ => throw new InvalidOperationException("The retried snapshot must be cached."),
            ct);

        // assert
        result.ShouldBeSameAs(fresh);
        cached.ShouldBeSameAs(fresh);
        loads.ShouldBe(2);
    }

    [Fact]
    public async Task Invalidate_LoadHeld_CompletesBeforeGateReleaseWithoutWaitingOnTheLoad()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var entered = NewSignal();
        var release = NewSignal();
        var ct = TestContext.Current.CancellationToken;

        async Task<GenreWeights> Load(CancellationToken token)
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            return Weights(4);
        }

        var inFlight = cache.GetOrLoadAsync(Load, ct);
        await entered.Task.WaitAsync(Timeout, ct);

        // act
        var invalidation = Task.Run(cache.Invalidate, ct);
        await invalidation.WaitAsync(Timeout, ct);
        var completedWhileHeld = invalidation.IsCompletedSuccessfully;
        release.SetResult();
        await inFlight.WaitAsync(Timeout, ct);

        // assert
        completedWhileHeld.ShouldBeTrue();
    }

    [Fact]
    public async Task GetOrLoadAsync_InvalidatedDuringLoad_WaitingCallerReceivesOnlyFreshSnapshot()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var stale = Weights(1);
        var fresh = Weights(5);
        var entered = NewSignal();
        var release = NewSignal();
        var loads = 0;
        var waiterLoads = 0;
        var ct = TestContext.Current.CancellationToken;

        async Task<GenreWeights> Load(CancellationToken token)
        {
            if (Interlocked.Increment(ref loads) == 1)
            {
                entered.TrySetResult();
                await release.Task.WaitAsync(token);
                return stale;
            }

            return fresh;
        }

        var holder = cache.GetOrLoadAsync(Load, ct);
        await entered.Task.WaitAsync(Timeout, ct);

        // act
        cache.Invalidate();
        var waiter = cache.GetOrLoadAsync(
            _ =>
            {
                Interlocked.Increment(ref waiterLoads);
                return Task.FromResult(stale);
            },
            ct);
        release.SetResult();
        var results = await Task.WhenAll(holder, waiter).WaitAsync(Timeout, ct);

        // assert
        results[0].ShouldBeSameAs(fresh);
        results[1].ShouldBeSameAs(fresh);
        loads.ShouldBe(2);
        waiterLoads.ShouldBe(0);
    }

    [Fact]
    public async Task GetOrLoadAsync_HolderCancelled_IndependentWaiterSurvivesAndSucceeds()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var expected = Weights(6);
        var entered = NewSignal();
        var holderLoads = 0;
        var waiterLoads = 0;
        var ct = TestContext.Current.CancellationToken;
        using var holderCancellation = new CancellationTokenSource();

        async Task<GenreWeights> HolderLoad(CancellationToken token)
        {
            Interlocked.Increment(ref holderLoads);
            entered.TrySetResult();
            await new TaskCompletionSource<GenreWeights>(TaskCreationOptions.RunContinuationsAsynchronously)
                .Task.WaitAsync(token);
            return expected;
        }

        var holder = cache.GetOrLoadAsync(HolderLoad, holderCancellation.Token);
        await entered.Task.WaitAsync(Timeout, ct);
        var waiter = cache.GetOrLoadAsync(
            _ =>
            {
                Interlocked.Increment(ref waiterLoads);
                return Task.FromResult(expected);
            },
            ct);

        // act
        await holderCancellation.CancelAsync();
        var cancellation = await Should.ThrowAsync<OperationCanceledException>(() => holder);
        var result = await waiter.WaitAsync(Timeout, ct);

        // assert
        cancellation.CancellationToken.ShouldBe(holderCancellation.Token);
        result.ShouldBeSameAs(expected);
        holderLoads.ShouldBe(1);
        waiterLoads.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrLoadAsync_RetryCancelled_ReleasesGateForANewCaller()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var entered = NewSignal();
        var release = NewSignal();
        var expected = Weights(7);
        var loads = 0;
        var ct = TestContext.Current.CancellationToken;
        using var cancellation = new CancellationTokenSource();

        async Task<GenreWeights> Load(CancellationToken _)
        {
            Interlocked.Increment(ref loads);
            entered.TrySetResult();
            await release.Task;
            return Weights(1);
        }

        var cancelled = cache.GetOrLoadAsync(Load, cancellation.Token);
        await entered.Task.WaitAsync(Timeout, ct);

        // act
        cache.Invalidate();
        await cancellation.CancelAsync();
        release.SetResult();
        await Should.ThrowAsync<OperationCanceledException>(() => cancelled);
        var result = await cache.GetOrLoadAsync(_ => Task.FromResult(expected), ct);

        // assert
        result.ShouldBeSameAs(expected);
        loads.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrLoadAsync_LoadThrows_DoesNotPublishAndReleasesGate()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var expected = Weights(8);
        var ct = TestContext.Current.CancellationToken;

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => cache.GetOrLoadAsync(
                _ => Task.FromException<GenreWeights>(new InvalidOperationException("catalog failure")),
                ct));
        var result = await cache.GetOrLoadAsync(_ => Task.FromResult(expected), ct);

        // assert
        failure.Message.ShouldBe("catalog failure");
        result.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task GetOrLoadAsync_LoadCancelled_DoesNotPublishAndReleasesGate()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var expected = Weights(9);
        var ct = TestContext.Current.CancellationToken;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var cancelledToken = cancellation.Token;

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => cache.GetOrLoadAsync(
                _ => Task.FromCanceled<GenreWeights>(cancelledToken),
                ct));
        var result = await cache.GetOrLoadAsync(_ => Task.FromResult(expected), ct);

        // assert
        result.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task GetOrLoadAsync_CallerTokenAlreadyCancelled_DoesNotInvokeLoader()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var invoked = false;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => cache.GetOrLoadAsync(
                _ =>
                {
                    invoked = true;
                    return Task.FromResult(Weights(10));
                },
                cancellation.Token));

        // assert
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task GetOrLoadAsync_CallerToken_IsPassedToTheLoader()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var expected = Weights(11);
        using var cancellation = new CancellationTokenSource();

        // act
        var result = await cache.GetOrLoadAsync(
            token =>
            {
                token.ShouldBe(cancellation.Token);
                return Task.FromResult(expected);
            },
            cancellation.Token);

        // assert
        result.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task GetOrLoadAsync_SnapshotAlreadyReturned_RemainsUnchangedAfterRebuild()
    {
        // arrange
        var cache = new GenreWeightsCache();
        var first = Weights(1);
        var second = Weights(2);
        var ct = TestContext.Current.CancellationToken;

        // act
        var returned = await cache.GetOrLoadAsync(_ => Task.FromResult(first), ct);
        cache.Invalidate();
        var rebuilt = await cache.GetOrLoadAsync(_ => Task.FromResult(second), ct);

        // assert
        returned.ShouldBeSameAs(first);
        returned.GetWeight(1).ShouldBeGreaterThan(1.0);
        rebuilt.ShouldBeSameAs(second);
        rebuilt.GetWeight(2).ShouldBeGreaterThan(1.0);
        ReferenceEquals(returned, rebuilt).ShouldBeFalse();
    }

    private static GenreWeights Weights(int genreId) =>
        GenreWeights.Calculate(
            new GenreCounts(10, new Dictionary<int, long> { [genreId] = 1 }));

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
