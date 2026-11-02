using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Import;

public sealed class ImportLibraryCommandHandlerPipelineTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task HandleAsync_UnderLoad_ConcurrencyReachesButNeverExceedsMaxConcurrency()
    {
        // arrange
        const int capacity = 3;
        var discovery = new DiscoveryDouble(Folders(9));
        var importer = new ImporterDouble(capacity, blockFromCall: 1);
        var handler = Handler(discovery, importer, capacity);

        // act
        var run = handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken);
        await importer.ReachedConcurrency.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        var observedWhileSaturated = importer.MaxInFlight;
        importer.Release();
        var result = await run.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        // assert
        observedWhileSaturated.ShouldBe(capacity);
        importer.MaxInFlight.ShouldBe(capacity);
        result.ImportedCount.ShouldBe(9);
    }

    [Fact]
    public async Task HandleAsync_FullChannel_ProducerAwaitsWriteAsyncWithoutPrefetchingTree()
    {
        // arrange
        const int capacity = 2;
        var discovery = new DiscoveryDouble(Folders(20)) { YieldedWindow = (capacity * 2) + 1 };
        var importer = new ImporterDouble(capacity, blockFromCall: 1);
        var handler = Handler(discovery, importer, capacity);

        // act
        var run = handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken);
        await importer.ReachedConcurrency.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await discovery.WindowReached.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        // assert
        discovery.YieldedCount.ShouldBe((capacity * 2) + 1);
        run.IsCompleted.ShouldBeFalse();
        importer.Release();
        var result = await run.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        result.ImportedCount.ShouldBe(20);
    }

    [Fact]
    public async Task HandleAsync_Drain_AwaitsAllTasksAndDisposesScopes()
    {
        // arrange
        const int capacity = 4;
        var discovery = new DiscoveryDouble(Folders(20));
        var importer = new ImporterDouble(capacity);
        var handler = Handler(discovery, importer, capacity);

        // act
        var result = await handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken);

        // assert
        result.ImportedCount.ShouldBe(20);
        importer.Calls.ShouldBe(20);
        importer.CreatedScopes.ShouldBe(20);
        importer.DisposedScopes.ShouldBe(20);
        importer.Imported.Count.ShouldBe(20);
    }

    [Fact]
    public async Task HandleAsync_CallerCancellation_AwaitsAllAndDisposesScopes()
    {
        // arrange
        const int capacity = 2;
        var discovery = new DiscoveryDouble(Folders(20));
        var importer = new ImporterDouble(capacity, blockFromCall: 1);
        var handler = Handler(discovery, importer, capacity);
        using var cancellation = new CancellationTokenSource();

        // act
        var run = handler.HandleAsync(new ImportLibraryCommand(), cancellation.Token);
        await importer.FirstCallEntered.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => run.WaitAsync(Timeout, TestContext.Current.CancellationToken));

        // assert
        importer.CreatedScopes.ShouldBe(importer.DisposedScopes);
    }

    [Fact]
    public async Task HandleAsync_ProducerFault_CancelsPeersAwaitsAllDisposesScopes()
    {
        // arrange
        var fault = new InvalidOperationException("discovery failed");
        var discovery = new DiscoveryDouble(Folders(20), _ => fault);
        var importer = new ImporterDouble(2);
        var handler = Handler(discovery, importer, 2);

        // act
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken));

        // assert
        thrown.ShouldBeSameAs(fault);
        importer.CreatedScopes.ShouldBe(importer.DisposedScopes);
    }

    [Fact]
    public async Task HandleAsync_ConsumerFault_CancelsPeersAwaitsAllDisposesScopes()
    {
        // arrange
        var fault = new InvalidOperationException("import failed");
        var discovery = new DiscoveryDouble(Folders(20));
        var importer = new ImporterDouble(2, failureAtCall: call => call == 2 ? fault : null);
        var handler = Handler(discovery, importer, 2);

        // act
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken));

        // assert
        thrown.ShouldBeSameAs(fault);
        importer.CreatedScopes.ShouldBe(importer.DisposedScopes);
    }

    [Fact]
    public async Task HandleAsync_CallerCancellationWhileWriterBlockedOnFullChannel_AwaitsAllDisposesScopesWithoutAbandonedWriter()
    {
        // arrange
        const int capacity = 2;
        var discovery = new DiscoveryDouble(Folders(100)) { YieldedWindow = (capacity * 2) + 1 };
        var importer = new ImporterDouble(capacity, blockFromCall: 1);
        var handler = Handler(discovery, importer, capacity);
        using var cancellation = new CancellationTokenSource();

        // act
        var run = handler.HandleAsync(new ImportLibraryCommand(), cancellation.Token);
        await discovery.WindowReached.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        run.IsCompleted.ShouldBeFalse();
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => run.WaitAsync(Timeout, TestContext.Current.CancellationToken));

        // assert
        discovery.YieldedCount.ShouldBeLessThan(100);
        importer.CreatedScopes.ShouldBe(importer.DisposedScopes);
    }

    [Fact]
    public async Task HandleAsync_ConsumerFaultWhileWriterBlockedOnFullChannel_CompletesWriterAwaitsAllDisposesScopes()
    {
        // arrange
        const int capacity = 2;
        var fault = new InvalidOperationException("consumer failed mid-stream");
        var discovery = new DiscoveryDouble(Folders(100)) { YieldedWindow = capacity + 1 };
        var importer = new ImporterDouble(capacity, blockFromCall: 1, failureAtCall: call => call == 2 ? fault : null);
        var handler = Handler(discovery, importer, capacity);

        // act
        var run = handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken);
        await discovery.WindowReached.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => run.WaitAsync(Timeout, TestContext.Current.CancellationToken));

        // assert
        thrown.ShouldBeSameAs(fault);
        discovery.YieldedCount.ShouldBeLessThan(100);
        importer.CreatedScopes.ShouldBe(importer.DisposedScopes);
    }

    [Fact]
    public async Task HandleAsync_CallerCancellationRace_PropagatesOperationCanceledExceptionAfterAwaitAllRetainingCommits()
    {
        // arrange
        var folders = Folders(4);
        var discovery = new DiscoveryDouble(folders);
        var importer = new ImporterDouble(1, blockFromCall: 2);
        var handler = Handler(discovery, importer, 1);
        using var cancellation = new CancellationTokenSource();

        // act
        var run = handler.HandleAsync(new ImportLibraryCommand(), cancellation.Token);
        await importer.SecondCallEntered.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        var thrown = await Should.ThrowAsync<OperationCanceledException>(
            () => run.WaitAsync(Timeout, TestContext.Current.CancellationToken));

        // assert
        thrown.CancellationToken.ShouldBe(cancellation.Token);
        importer.Imported.ShouldContain(folders[0]);
        importer.CreatedScopes.ShouldBe(importer.DisposedScopes);
    }

    [Fact]
    public async Task HandleAsync_FirstGenuineFaultRace_PropagatesOriginalExceptionIdentityAndStackNotPeerCancellationOrAggregate()
    {
        // arrange
        var origin = new InvalidOperationException("first genuine fault");
        var discovery = new DiscoveryDouble(Folders(20));
        var importer = new ImporterDouble(2, failureAtCall: call => call == 1 ? origin : null);
        var handler = Handler(discovery, importer, 2);

        // act
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportLibraryCommand(), TestContext.Current.CancellationToken));

        // assert
        thrown.ShouldBeSameAs(origin);
        thrown.ShouldNotBeOfType<AggregateException>();
        thrown.StackTrace.ShouldNotBeNull().ShouldContain(nameof(ImporterDouble));
        importer.CreatedScopes.ShouldBe(importer.DisposedScopes);
    }

    private static ImportLibraryCommandHandler Handler(
        DiscoveryDouble discovery,
        ImporterDouble importer,
        int maxConcurrency) =>
        new(
            discovery,
            importer,
            Microsoft.Extensions.Options.Options.Create(
                new LibraryOptions { RootPath = "/library", MaxConcurrency = maxConcurrency }));

    private static IReadOnlyList<LibraryPath> Folders(int count) =>
        [.. Enumerable.Range(0, count).Select(index => new LibraryPath($"movie-{index:000}"))];
}
