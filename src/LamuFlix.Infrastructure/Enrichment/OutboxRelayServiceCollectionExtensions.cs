using System;
using LamuFlix.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Enrichment;

public static class OutboxRelayServiceCollectionExtensions
{
    public static IServiceCollection AddLamuFlixOutboxRelay(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<OutboxRelayOptions>()
            .BindConfiguration(OutboxRelayOptions.SectionName)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<OutboxRelayOptions>, OutboxRelayOptionsValidator>());
        services.TryAddSingleton<OutboxRelayState>();
        services.TryAddSingleton<OutboxRelayMetrics>();
        services.AddHostedService<OutboxRelayHostedService>();
        services.AddHealthChecks()
            .AddCheck<OutboxRelayHealthCheck>(OutboxRelayHealthCheck.HealthCheckName, tags: [HealthCheckTags.Ready]);

        return services;
    }
}
