using System;
using System.Net.Http;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LamuFlix.Infrastructure.Adapters;

public static class SubtitleProviderServiceCollectionExtensions
{
    public static IServiceCollection AddSubtitleProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services
            .AddHttpClient<ISubtitleProvider, OpenSubtitlesSubtitleProvider>()
            .ConfigurePrimaryHttpMessageHandler(static () => new HttpClientHandler { AllowAutoRedirect = false })
            .ConfigureHttpClient(static client => client.Timeout = System.Threading.Timeout.InfiniteTimeSpan);

        return services;
    }
}
