using System;
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

        return services;
    }
}
