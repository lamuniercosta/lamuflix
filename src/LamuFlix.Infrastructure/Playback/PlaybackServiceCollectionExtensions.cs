using System;
using System.IO.Abstractions;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Playback;

public static class PlaybackServiceCollectionExtensions
{
    public static IServiceCollection AddLamuFlixPlayback(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IProcessStarter, SystemProcessStarter>();
        services.AddSingleton<IMediaPlayerLauncher>(static provider =>
        {
            var features = provider.GetRequiredService<IOptions<FeatureOptions>>().Value;
            if (!features.LocalPlay)
            {
                return new DisabledMediaPlayerLauncher();
            }

            var playback = provider.GetRequiredService<IOptions<PlaybackOptions>>();
            var starter = provider.GetRequiredService<IProcessStarter>();
            var fileSystem = provider.GetRequiredService<IFileSystem>();
            return new ProcessMediaPlayerLauncher(playback, starter, fileSystem);
        });

        return services;
    }
}
