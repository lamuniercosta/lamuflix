using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using LamuFlix.Data.Models;
using LamuFlix.Worker.Services;

namespace LamuFlix.Worker;

public sealed class QueueWorker(
    ILogger<QueueWorker> logger,
    IConfiguration configuration,
    IEnrichmentJobProcessor? jobProcessor = null,
    IConnectionFactory? connectionFactory = null,
    TimeProvider? timeProvider = null)
    : BackgroundService
{
    private readonly ILogger<QueueWorker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QueueWorker starting at: {Time}", _timeProvider.GetUtcNow());

        var hostName = _configuration["RabbitMQ:HostName"] ?? "localhost";
        var queueName = _configuration["RabbitMQ:QueueName"] ?? "task_queue";
        var dlqName = _configuration["RabbitMQ:DlqName"] ?? "task_queue_dlq";

        try
        {
            await RunListenerAsync(hostName, queueName, dlqName, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("QueueWorker cancellation requested.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running the worker service.");
        }
        finally
        {
            _logger.LogInformation("QueueWorker stopped at: {Time}", _timeProvider.GetUtcNow());
        }
    }

    private async Task RunListenerAsync(string hostName, string queueName, string dlqName, CancellationToken stoppingToken)
    {
        var factory = connectionFactory ?? new ConnectionFactory { HostName = hostName };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        ConfigureQueues(channel, queueName, dlqName);
        RegisterConsumer(channel, queueName, stoppingToken);

        _logger.LogInformation("QueueWorker listening on queue '{Queue}' on '{Host}'.", queueName, hostName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static void ConfigureQueues(IModel channel, string queueName, string dlqName)
    {
        channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueDeclare(queue: dlqName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
    }

    private void RegisterConsumer(IModel channel, string queueName, CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<MovieEnrichmentMessage>(json);

                if (message == null)
                {
                    _logger.LogWarning("Received null or invalid message payload. Acknowledging.");
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                if (jobProcessor != null)
                {
                    await jobProcessor.ProcessMessageAsync(message, channel, ea.DeliveryTag);
                }
                else
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse or dispatch message with delivery tag {Tag}.", ea.DeliveryTag);
                try
                {
                    channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch
                {
                    // ignored
                }
            }
        };

        channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }
}