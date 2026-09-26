using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using LamuFlix.Data.Models;

namespace LamuFlix.Web.Services;

public class RabbitMqEnrichmentQueuePublisher(
    IConfiguration configuration,
    IConnectionFactory? connectionFactory = null)
    : IEnrichmentQueuePublisher
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public Task PublishAsync(MovieEnrichmentMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var hostName = _configuration["RabbitMQ:HostName"] ?? "localhost";
        var queueName = _configuration["RabbitMQ:QueueName"] ?? "task_queue";

        var factory = connectionFactory ?? new ConnectionFactory { HostName = hostName };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }
}