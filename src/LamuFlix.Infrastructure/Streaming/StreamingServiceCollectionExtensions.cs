using System;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Streaming;

public static class StreamingServiceCollectionExtensions
{
    public static IServiceCollection AddLamuFlixStreaming(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<StreamConversionOptions>()
            .BindConfiguration(StreamConversionOptions.SectionName)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<StreamConversionOptions>, StreamConversionOptionsValidator>());
        services.AddSingleton<IStreamConversionService>(static provider =>
            new StreamConversionService(
                provider.GetRequiredService<IOptions<StreamConversionOptions>>(),
                provider.GetRequiredService<IOptions<ThumbnailOptions>>(),
                provider.GetRequiredService<IProcessRunner>(),
                provider.GetRequiredService<IHostEnvironment>(),
                provider.GetRequiredService<ILogger<StreamConversionService>>(),
                provider.GetService<TimeProvider>()));

        return services;
    }
}
