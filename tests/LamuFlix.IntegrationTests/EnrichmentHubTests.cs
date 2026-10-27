using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LamuFlix.Api;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class EnrichmentHubTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const string HubRoute = "/hubs/enrichment";
    private const string NegotiateRoute = "/hubs/enrichment/negotiate?negotiateVersion=1";
    private const string EventName = "EnrichmentStatusChanged";
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task Negotiate_ReturnsConnectionTokenAndTransports()
    {
        // arrange
        using var client = factory.CreateClient();
        using var content = new StringContent(string.Empty);

        // act
        var response = await client.PostAsync(new Uri(NegotiateRoute, UriKind.Relative), content, TestContext.Current.CancellationToken);

        // assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        document.RootElement.GetProperty("connectionToken").GetString().ShouldNotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("availableTransports").GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void LiveHost_ResolvesRealSignalRNotifier()
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var notifier = scope.ServiceProvider.GetRequiredService<IEnrichmentStatusNotifier>();

        // assert
        notifier.ShouldBeOfType<SignalREnrichmentStatusNotifier>();
    }

    [Fact]
    public async Task ConnectedClient_CommittedStatuses_ReceivesCamelCaseFramedEvents()
    {
        // arrange
        var received = Channel.CreateUnbounded<JsonElement>();
        await using var connection = BuildConnection();
        connection.On<JsonElement>(EventName, payload => received.Writer.TryWrite(payload));
        await connection.StartAsync(TestContext.Current.CancellationToken);
        var notifier = factory.Services.GetRequiredService<IEnrichmentStatusNotifier>();
        var ids = new[] { 101, 102, 103, 104 };
        var statuses = new[]
        {
            EnrichmentStatus.Pending,
            EnrichmentStatus.Enriched,
            EnrichmentStatus.NotFound,
            EnrichmentStatus.Failed,
        };

        // act
        for (var index = 0; index < statuses.Length; index++)
        {
            await notifier.NotifyAsync(new MovieId(ids[index]), statuses[index], CancellationToken.None);
        }

        // assert
        for (var index = 0; index < statuses.Length; index++)
        {
            var payload = await ReceiveAsync(received.Reader);
            payload.GetProperty("id").ValueKind.ShouldBe(JsonValueKind.Number);
            payload.GetProperty("id").GetInt32().ShouldBe(ids[index]);
            payload.GetProperty("enrichmentStatus").GetString().ShouldBe(statuses[index].Name);
            payload.TryGetProperty("Id", out _).ShouldBeFalse();
        }
    }

    private HubConnection BuildConnection() =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, HubRoute), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();

    private static async Task<JsonElement> ReceiveAsync(ChannelReader<JsonElement> reader)
    {
        using var timeout = new CancellationTokenSource(ReceiveTimeout);
        return await reader.ReadAsync(timeout.Token);
    }
}
