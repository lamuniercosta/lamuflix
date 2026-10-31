using System;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Thumbnails;

public static class ThumbnailServiceCollectionExtensions
{
    public static IServiceCollection AddLamuFlixThumbnails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IProcessRunner>(static provider =>
            new SystemProcessRunner(provider.GetService<TimeProvider>()));
        services.AddSingleton<IThumbnailGenerator>(static provider =>
            new FfmpegThumbnailGenerator(
                provider.GetRequiredService<IOptions<ThumbnailOptions>>(),
                provider.GetRequiredService<IProcessRunner>(),
                provider.GetRequiredService<IHostEnvironment>()));

        return services;
    }
}
