using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class InactiveThumbnailConsumer(ILogger<InactiveThumbnailConsumer> logger) : IHostedService
{
    public const string InactiveLogMessage =
        "The RabbitMQ thumbnail consumer is inactive because Thumbnails:Enabled is false. Messages wait in " +
        RabbitMqTopology.ThumbnailQueue +
        ".";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{Message}", InactiveLogMessage);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
