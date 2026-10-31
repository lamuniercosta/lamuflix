using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class ThumbnailConsumer : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
