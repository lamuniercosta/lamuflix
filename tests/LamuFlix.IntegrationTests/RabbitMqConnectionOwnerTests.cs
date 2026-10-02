using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(RabbitMqCollection))]
public sealed class RabbitMqConnectionOwnerTests : IAsyncLifetime
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    private readonly RabbitMqFixture fixture;

    public RabbitMqConnectionOwnerTests(RabbitMqFixture fixture)
    {
        this.fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await fixture.ResetTopologyAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetAsync_CalledTwice_ReturnsTheSameOpenConnection()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var owner = NewOwner(fixture.Options);

        var first = await owner.GetAsync(ct);
        var second = await owner.GetAsync(ct);

        second.ShouldBeSameAs(first);
        first.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_UnreachableBroker_SurfacesTheFailure()
    {
        using var bounded = new CancellationTokenSource(Bound);
        await using var owner = NewOwner(Unreachable());

        await Should.ThrowAsync<Exception>(() => owner.GetAsync(bounded.Token));
    }

    [Fact]
    public async Task GetAsync_AfterAFailedCreation_SucceedsOnALaterCall()
    {
        var options = new MutableOptions(Unreachable());
        await using var owner = new RabbitMqConnectionOwner(options);
        using (var bounded = new CancellationTokenSource(Bound))
        {
            await Should.ThrowAsync<Exception>(() => owner.GetAsync(bounded.Token));
        }

        options.Value = fixture.Options;
        var connection = await owner.GetAsync(TestContext.Current.CancellationToken);

        connection.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_ConcurrentCallers_ShareOneConnection()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var owner = NewOwner(fixture.Options);

        var pending = new Task<IConnection>[4];
        for (var index = 0; index < pending.Length; index++)
        {
            pending[index] = owner.GetAsync(ct);
        }

        var connections = await Task.WhenAll(pending);

        connections.ShouldAllBe(connection => connection.IsOpen);
        connections.Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public async Task DisposeAsync_ClosesTheConnection()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = NewOwner(fixture.Options);
        var connection = await owner.GetAsync(ct);

        await owner.DisposeAsync();

        connection.IsOpen.ShouldBeFalse();
    }

    private sealed class MutableOptions(RabbitMqOptions value) : IOptions<RabbitMqOptions>
    {
        public RabbitMqOptions Value { get; set; } = value;
    }

    private RabbitMqOptions Unreachable() => fixture.Options with { Port = ClosedPort() };

    private static int ClosedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static RabbitMqConnectionOwner NewOwner(RabbitMqOptions options) =>
        new(Options.Create(options));
}

