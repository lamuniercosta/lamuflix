using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.IntegrationTests;

internal sealed class RecordingEnrichmentStatusNotifier : IEnrichmentStatusNotifier
{
    private readonly List<Notification> notifications = [];

    public Func<MovieId, EnrichmentStatus, Task>? OnNotify { get; set; }

    public IReadOnlyList<Notification> Notifications => notifications;

    public async Task NotifyAsync(MovieId movieId, EnrichmentStatus status, CancellationToken cancellationToken)
    {
        notifications.Add(new Notification(movieId, status, cancellationToken));
        if (OnNotify is { } onNotify)
        {
            await onNotify(movieId, status);
        }
    }

    internal sealed record Notification(MovieId MovieId, EnrichmentStatus Status, CancellationToken CancellationToken);
}
