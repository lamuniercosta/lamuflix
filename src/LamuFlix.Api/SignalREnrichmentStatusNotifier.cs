using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LamuFlix.Api;

public sealed class SignalREnrichmentStatusNotifier(
    IHubContext<EnrichmentHub> hubContext,
    ILogger<SignalREnrichmentStatusNotifier> logger) : IEnrichmentStatusNotifier
{
    private const string EventName = "EnrichmentStatusChanged";

    public async Task NotifyAsync(MovieId movieId, EnrichmentStatus status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(movieId);
        ArgumentNullException.ThrowIfNull(status);

        logger.LogInformation(
            "Broadcasting {EnrichmentStatusChanged} for movie {MovieId} with committed status {EnrichmentStatus}.",
            EventName,
            movieId.Value,
            status.Name);

        try
        {
            await hubContext.Clients.All.SendAsync(
                EventName,
                new EnrichmentStatusChangedDto(movieId.Value, status),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to broadcast {EnrichmentStatusChanged} for movie {MovieId} with committed status {EnrichmentStatus}.",
                EventName,
                movieId.Value,
                status.Name);
        }
    }
}
