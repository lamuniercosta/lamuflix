using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Domain;

public sealed class KeywordWeightsCacheTests
{
    [Fact]
    public async Task GetOrLoadAsync_CachesSuccessfulLoadAndRetriesFailure()
    {
        var cache = new KeywordWeightsCache();
        var expected = new KeywordCounts(1, new Dictionary<string, long> { ["Drama"] = 1 });
        var failed = await Should.ThrowAsync<InvalidOperationException>(
            () => cache.GetOrLoadAsync(_ => throw new InvalidOperationException("failed"), TestContext.Current.CancellationToken));

        failed.Message.ShouldBe("failed");
        var loaded = await cache.GetOrLoadAsync(_ => Task.FromResult(expected), TestContext.Current.CancellationToken);
        var cached = await cache.GetOrLoadAsync(_ => throw new InvalidOperationException("cache miss"), TestContext.Current.CancellationToken);

        loaded.ShouldBeSameAs(expected);
        cached.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task GetOrLoadAsync_InvalidatedDuringLoad_DiscardsStaleResult()
    {
        var cache = new KeywordWeightsCache();
        var stale = new KeywordCounts(1, new Dictionary<string, long>());
        var current = new KeywordCounts(2, new Dictionary<string, long> { ["Drama"] = 1 });
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;

        async Task<KeywordCounts> Load(CancellationToken token)
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.SetResult();
                await release.Task.WaitAsync(token);
                return stale;
            }

            return current;
        }

        var read = cache.GetOrLoadAsync(Load, TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        cache.Invalidate();
        release.SetResult();

        (await read).ShouldBeSameAs(current);
        calls.ShouldBe(2);
    }

    [Fact]
    public async Task GetOrLoadAsync_CancellationDuringStaleRetry_DoesNotPublish()
    {
        var cache = new KeywordWeightsCache();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var calls = 0;

        async Task<KeywordCounts> Load(CancellationToken token)
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.SetResult();
                await release.Task.WaitAsync(token);
            }

            return new KeywordCounts(0, new Dictionary<string, long>());
        }

        var read = cache.GetOrLoadAsync(Load, cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        cache.Invalidate();
        await cancellation.CancelAsync();
        release.SetResult();

        await Should.ThrowAsync<OperationCanceledException>(() => read);
        calls.ShouldBe(1);
    }
}