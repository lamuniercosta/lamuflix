using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Api;
using LamuFlix.Core.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LamuFlix.UnitTests.Enrichment;

public sealed class SignalREnrichmentStatusNotifierTests
{
    private const string EventName = "EnrichmentStatusChanged";

    public static TheoryData<string> StatusNames =>
        new()
        {
            nameof(EnrichmentStatus.Pending),
            nameof(EnrichmentStatus.Enriched),
            nameof(EnrichmentStatus.NotFound),
            nameof(EnrichmentStatus.Failed),
        };

    [Theory]
    [MemberData(nameof(StatusNames))]
    public async Task NotifyAsync_CommittedStatus_BroadcastsMappedDtoToAllClients(string statusName)
    {
        // arrange
        var status = EnrichmentStatus.FromName(statusName);
        var (notifier, all) = CreateNotifier();
        object?[]? sent = null;
        all.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<object?[]>();
                return Task.CompletedTask;
            });
        var movieId = new MovieId(42);
        var token = new CancellationTokenSource().Token;

        // act
        await notifier.NotifyAsync(movieId, status, token);

        // assert
        await all.Received(1).SendCoreAsync(EventName, Arg.Any<object?[]>(), token);
        sent.ShouldNotBeNull();
        var dto = sent[0].ShouldBeOfType<EnrichmentStatusChangedDto>();
        dto.Id.ShouldBe(42);
        dto.EnrichmentStatus.ShouldBeSameAs(status);
    }

    [Fact]
    public async Task NotifyAsync_SendThrows_IsolatesFailureAndDoesNotRethrow()
    {
        // arrange
        var (notifier, all) = CreateNotifier();
        all.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("transport down")));

        // act
        var exception = await Record.ExceptionAsync(
            () => notifier.NotifyAsync(new MovieId(7), EnrichmentStatus.Failed, CancellationToken.None));

        // assert
        exception.ShouldBeNull();
        await all.Received(1).SendCoreAsync(
            EventName,
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    private static (SignalREnrichmentStatusNotifier Notifier, IClientProxy All) CreateNotifier()
    {
        var all = Substitute.For<IClientProxy>();
        var clients = Substitute.For<IHubClients>();
        clients.All.Returns(all);
        var hubContext = Substitute.For<IHubContext<EnrichmentHub>>();
        hubContext.Clients.Returns(clients);
        var notifier = new SignalREnrichmentStatusNotifier(
            hubContext,
            NullLogger<SignalREnrichmentStatusNotifier>.Instance);
        return (notifier, all);
    }
}
