using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace LamuFlix.Infrastructure.RabbitMq;

public static class RabbitMqServiceCollectionExtensions
{
    public const string InactiveLogMessage =
        "The RabbitMQ enrichment consumer is inactive because no IMetadataProvider and IMovieRepository are both registered. Messages wait in " +
        RabbitMqTopology.RequestedQueue +
        ".";

    public static IServiceCollection AddLamuFlixRabbitMq(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Sdk.SetDefaultTextMapPropagator(new TraceContextPropagator());

        services.AddSingleton<RabbitMqConnectionOwner>();
        services.AddSingleton<RabbitMqTopology>();
        services.AddSingleton<RabbitMqEnrichmentQueuePublisher>();
        services.AddSingleton<IEnrichmentQueue>(provider => provider.GetRequiredService<RabbitMqEnrichmentQueuePublisher>());

        // The matching AddCheck lives in RabbitMqServiceCollectionExtensions; AddHealthChecks() is in ServiceDefaults.
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: [HealthCheckTags.Ready]);

        if (IsConsumerActive(services))
        {
            services.AddScoped<ProcessEnrichmentCommandHandler>();
            services.AddScoped<ProcessEnrichmentCommandValidator>();
            services.AddScoped<IValidator<ProcessEnrichmentCommand>>(provider =>
                provider.GetRequiredService<ProcessEnrichmentCommandValidator>());
            services.AddHandler<ProcessEnrichmentCommandHandler, ProcessEnrichmentCommand, ProcessEnrichmentOutcome>();
            services.AddSingleton<IValidateOptions<RabbitMqOptions>, RabbitMqConsumerOptionsValidator>();
            services.AddOptions<RabbitMqOptions>().ValidateOnStart();
            services.TryAddSingleton<IFileSystem, System.IO.Abstractions.FileSystem>();
            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton<IWorkerLiveness>(provider => new WorkerLivenessSignal(
                provider.GetRequiredService<IFileSystem>(),
                provider.GetRequiredService<TimeProvider>(),
                AppContext.BaseDirectory));
            services.AddHostedService<EnrichmentConsumer>();
            return services;
        }

        services.AddHostedService<InactiveEnrichmentConsumer>();
        return services;
    }

    private static bool IsConsumerActive(IServiceCollection services) =>
        HasRegistration<IMetadataProvider>(services) && HasRegistration<IMovieRepository>(services);

    private static bool HasRegistration<TPort>(IServiceCollection services) =>
        services.Any(descriptor => descriptor.ServiceType == typeof(TPort));

    /// Declared here rather than in its own file: no logger exists while the container is being built.
    private sealed class InactiveEnrichmentConsumer(ILogger<InactiveEnrichmentConsumer> logger) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("{Message}", InactiveLogMessage);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
