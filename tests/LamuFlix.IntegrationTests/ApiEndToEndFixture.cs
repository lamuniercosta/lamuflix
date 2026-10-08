using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class ApiEndToEndFixture : IAsyncLifetime
{
    private static readonly HttpClient WarmUpClient = new();

    private readonly PostgresFixture postgres = new();
    private readonly RabbitMqFixture rabbitMq = new();

    private WireMockServer? server;

    public PostgresFixture Postgres => postgres;

    public RabbitMqFixture RabbitMq => rabbitMq;

    public WireMockServer Server => server ?? throw new InvalidOperationException(
        "ApiEndToEndFixture has not been prepared. PrepareAsync must run first.");

    public async ValueTask InitializeAsync()
    {
        await postgres.InitializeAsync();
        await rabbitMq.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        StopServer();
        await rabbitMq.DisposeAsync();
        await postgres.DisposeAsync();
    }

    public async Task PrepareAsync(CancellationToken cancellationToken)
    {
        await MigrateAsync(cancellationToken);
        await postgres.ResetAsync(cancellationToken);
        await rabbitMq.ResetTopologyAsync(cancellationToken);
        StartServer();
        using var warmUp = await WarmUpClient.GetAsync(new Uri(Server.Urls[0]), cancellationToken);
        Server.Reset();
    }

    public ApiEndToEndHostSettings HostSettings(TimeProvider timeProvider) =>
        new(postgres.ConnectionString, rabbitMq.Options, Server.Urls[0], timeProvider);

    private async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using var context = postgres.CreateMigratedContext();
        await context.Database.MigrateAsync(cancellationToken);
    }

    private void StartServer()
    {
        StopServer();
        server = WireMockServer.Start();
    }

    private void StopServer()
    {
        if (server is not { } started)
        {
            return;
        }

        server = null;
        started.Stop();
        started.Dispose();
    }
}
