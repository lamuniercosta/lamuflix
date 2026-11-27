using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;

namespace LamuFlix.UnitTests.Adapters;

public sealed class MetadataQuotaHandlerTests
{
    [Fact]
    public async Task SendAsync_Admitted_DisposesScopeBeforeTransportAndReturnsResponse()
    {
        // arrange
        var gate = new StubGate(_ => Task.FromResult(true));
        var factory = new TrackingScopeFactory(() => gate);
        bool? disposedAtTransport = null;
        var transport = new RecordingHandler(() => disposedAtTransport = factory.Scopes[0].Disposed);
        using var client = Client(factory, transport);

        // act
        using var response = await client.GetAsync(new Uri("https://example.test/?t=Blade"), TestContext.Current.CancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        disposedAtTransport.ShouldBe(true);
        factory.Scopes.ShouldHaveSingleItem().Disposed.ShouldBeTrue();
        transport.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task SendAsync_GateDenies_ThrowsExhaustionWithoutDispatchAndDisposesScope()
    {
        // arrange
        var factory = new TrackingScopeFactory(() => new StubGate(_ => Task.FromResult(false)));
        var transport = new RecordingHandler();
        using var client = Client(factory, transport);

        // act
        var failure = await Should.ThrowAsync<MetadataQuotaExhaustedException>(
            () => client.GetAsync(new Uri("https://example.test/?t=Blade"), TestContext.Current.CancellationToken));

        // assert
        failure.InnerException.ShouldBeNull();
        transport.Calls.ShouldBe(0);
        factory.Scopes.ShouldHaveSingleItem().Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_GateFaults_ThrowsPersistenceSignalWithInnerAndDisposesWithoutDispatch()
    {
        // arrange
        var fault = new InvalidOperationException("database unavailable");
        var factory = new TrackingScopeFactory(() => new StubGate(_ => Task.FromException<bool>(fault)));
        var transport = new RecordingHandler();
        using var client = Client(factory, transport);

        // act
        var failure = await Should.ThrowAsync<MetadataQuotaPersistenceException>(
            () => client.GetAsync(new Uri("https://example.test/?t=Blade"), TestContext.Current.CancellationToken));

        // assert
        failure.InnerException.ShouldBeSameAs(fault);
        transport.Calls.ShouldBe(0);
        factory.Scopes.ShouldHaveSingleItem().Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_GateCancels_PropagatesCancellationAndDisposesWithoutDispatch()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        var factory = new TrackingScopeFactory(
            () => new StubGate(ct => Task.FromException<bool>(new OperationCanceledException(ct))));
        var transport = new RecordingHandler();
        using var client = Client(factory, transport);

        // act
        var failure = await Should.ThrowAsync<OperationCanceledException>(
            () => client.GetAsync(new Uri("https://example.test/?t=Blade"), cancellation.Token));

        // assert
        failure.ShouldBeAssignableTo<OperationCanceledException>();
        failure.ShouldNotBeOfType<MetadataQuotaPersistenceException>();
        transport.Calls.ShouldBe(0);
        factory.Scopes.ShouldHaveSingleItem().Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_TwoAttempts_CreatesFreshScopeAndGatePerAttempt()
    {
        // arrange
        var gates = new List<StubGate>();
        var factory = new TrackingScopeFactory(() =>
        {
            var gate = new StubGate(_ => Task.FromResult(true));
            gates.Add(gate);
            return gate;
        });
        var transport = new RecordingHandler();
        using var client = Client(factory, transport);

        // act
        using var first = await client.GetAsync(new Uri("https://example.test/?t=First"), TestContext.Current.CancellationToken);
        using var second = await client.GetAsync(new Uri("https://example.test/?t=Second"), TestContext.Current.CancellationToken);

        // assert
        factory.Scopes.Count.ShouldBe(2);
        gates.Count.ShouldBe(2);
        gates[0].ShouldNotBeSameAs(gates[1]);
        factory.Scopes.ShouldAllBe(scope => scope.Disposed);
        transport.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task SendAsync_AmbientToken_PassesSendAsyncCancellationTokenToGate()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        StubGate? gate = null;
        var factory = new TrackingScopeFactory(() => gate = new StubGate(_ => Task.FromResult(true)));
        using var invoker = new HttpMessageInvoker(new MetadataQuotaHandler(factory) { InnerHandler = new RecordingHandler() });

        // act
        using var response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, new Uri("https://example.test/?t=Blade")),
            cancellation.Token);

        // assert
        gate.ShouldNotBeNull();
        gate.Token.ShouldBe(cancellation.Token);
    }

    [Fact]
    public async Task SendAsync_DiscoveryCeilingExhausted_ThrowsBeforeGateOrDispatch()
    {
        // arrange
        var calls = 0;
        var factory = new TrackingScopeFactory(() => new StubGate(_ =>
        {
            calls++;
            return Task.FromResult(true);
        }));
        var transport = new RecordingHandler();
        using var invoker = new HttpMessageInvoker(new MetadataQuotaHandler(factory) { InnerHandler = transport });
        var budget = new OutsideLibraryEvidenceBudget(1, new DateOnly(2026, 7, 15));
        using var first = RequestWithBudget(budget);
        using var second = RequestWithBudget(budget);

        // act
        using var response = await invoker.SendAsync(first, TestContext.Current.CancellationToken);
        var failure = await Should.ThrowAsync<MetadataLoadCeilingExhaustedException>(
            () => invoker.SendAsync(second, TestContext.Current.CancellationToken));

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        failure.ShouldBeOfType<MetadataLoadCeilingExhaustedException>();
        calls.ShouldBe(1);
        transport.Calls.ShouldBe(1);
        budget.UsedSends.ShouldBe(1);
    }

    [Fact]
    public async Task SendAsync_RequestWithoutBudgetOption_IgnoresLoadCeiling()
    {
        // arrange
        var factory = new TrackingScopeFactory(() => new StubGate(_ => Task.FromResult(true)));
        var transport = new RecordingHandler();
        using var invoker = new HttpMessageInvoker(new MetadataQuotaHandler(factory) { InnerHandler = transport });
        var budget = new OutsideLibraryEvidenceBudget(0, new DateOnly(2026, 7, 15));

        // act
        using var response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, new Uri("https://example.test/?t=Blade")),
            TestContext.Current.CancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        transport.Calls.ShouldBe(1);
        budget.UsedSends.ShouldBe(0);
    }

    [Fact]
    public void Constructor_DependsOnlyOnScopeFactory()
    {
        // arrange
        var constructor = typeof(MetadataQuotaHandler).GetConstructors().ShouldHaveSingleItem();

        // act
        var parameters = constructor.GetParameters();

        // assert
        parameters.ShouldHaveSingleItem().ParameterType.ShouldBe(typeof(IServiceScopeFactory));
    }

    private static HttpClient Client(IServiceScopeFactory factory, HttpMessageHandler transport) =>
        new(new MetadataQuotaHandler(factory) { InnerHandler = transport });

    private static HttpRequestMessage RequestWithBudget(OutsideLibraryEvidenceBudget budget)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://example.test/?t=Discovery"));
        request.Options.Set(OutsideLibraryEvidenceBudget.DiscoveryBudgetKey, budget);
        return request;
    }

    private sealed class StubGate(Func<CancellationToken, Task<bool>> acquire) : IOmdbRequestGate
    {
        public CancellationToken Token { get; private set; }

        public Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            return acquire(cancellationToken);
        }
    }

    private sealed class TrackingScopeFactory(Func<IOmdbRequestGate> gates) : IServiceScopeFactory
    {
        private readonly List<TrackingScope> scopes = [];

        public IReadOnlyList<TrackingScope> Scopes => scopes;

        public IServiceScope CreateScope()
        {
            var scope = new TrackingScope(gates());
            scopes.Add(scope);
            return scope;
        }
    }

    private sealed class TrackingScope(IOmdbRequestGate gate) : IServiceScope, IServiceProvider
    {
        public bool Disposed { get; private set; }

        public IServiceProvider ServiceProvider => this;

        public object? GetService(Type serviceType) =>
            serviceType == typeof(IOmdbRequestGate) ? gate : null;

        public void Dispose() => Disposed = true;
    }

    private sealed class RecordingHandler(Action? onSend = null) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            onSend?.Invoke();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "Response": "True", "Title": "Blade Runner" }"""),
            });
        }
    }
}
