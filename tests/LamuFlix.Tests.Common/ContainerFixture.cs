using System;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace LamuFlix.Tests.Common;

public static class ContainerFixture
{
    private const string DockerNotStartedMessage =
        "Docker not available; ContainerFixture.StartAsync() must run first";

    private static PostgreSqlContainer? postgresContainer;
    private static RabbitMqContainer? rabbitMqContainer;

    public static async Task StartAsync()
    {
        postgresContainer = new PostgreSqlBuilder("postgres:16.4").Build();
        rabbitMqContainer = new RabbitMqBuilder("rabbitmq:4.0.0").Build();

        await postgresContainer.StartAsync();
        await rabbitMqContainer.StartAsync();
    }

    public static async ValueTask DisposeAsync()
    {
        if (postgresContainer is not null)
        {
            await postgresContainer.DisposeAsync();
        }

        if (rabbitMqContainer is not null)
        {
            await rabbitMqContainer.DisposeAsync();
        }
    }

    public static PostgreSqlContainer Postgres =>
        postgresContainer ?? throw new InvalidOperationException(DockerNotStartedMessage);

    public static RabbitMqContainer RabbitMq =>
        rabbitMqContainer ?? throw new InvalidOperationException(DockerNotStartedMessage);
}
