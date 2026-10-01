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
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class RabbitMqConnectionOwnerTests : IClassFixture<RabbitMqFixture>
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    private readonly RabbitMqFixture fixture;

    public RabbitMqConnectionOwnerTests(RabbitMqFixture fixture)
    {
        this.fixture = fixture;
    }

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
        var ct = TestContext.Current.CancellationToken;
        var owner = NewOwner(Unreachable());
        using (var bounded = new CancellationTokenSource(Bound))
        {
            await Should.ThrowAsync<Exception>(() => owner.GetAsync(bounded.Token));
        }

        var recovered = NewOwner(fixture.Options);
        var connection = await recovered.GetAsync(ct);

        connection.IsOpen.ShouldBeTrue();
        await recovered.DisposeAsync();
        await owner.DisposeAsync();
    }

    [Fact]
    public async Task GetAsync_ConcurrentCallers_ShareOneConnection()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var owner = NewOwner(fixture.Options);

        var connections = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => owner.GetAsync(ct)));

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

