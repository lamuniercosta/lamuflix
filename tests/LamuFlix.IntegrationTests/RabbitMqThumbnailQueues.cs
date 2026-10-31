using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;

namespace LamuFlix.IntegrationTests;

internal static class RabbitMqThumbnailQueues
{
    public static async Task PurgeThumbnailQueuesAsync(RabbitMqFixture fixture, CancellationToken cancellationToken)
    {
        await using var connection = await fixture.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueuePurgeAsync(RabbitMqTopology.ThumbnailQueue, cancellationToken);
        await channel.QueuePurgeAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, cancellationToken);
    }
}
