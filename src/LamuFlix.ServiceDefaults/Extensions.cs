using System;
using System.Linq;
using LamuFlix.Core.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;

namespace LamuFlix.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (builder.Services.Any(static descriptor => descriptor.ServiceType == typeof(ServiceDefaultsMarker)))
        {
            return builder;
        }

        builder.Services.AddSingleton<ServiceDefaultsMarker>();
        builder.Services.AddLamuFlixOptions();
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Logging.ClearProviders();
        builder.Host.UseSerilog(
            (_, loggerConfiguration) => loggerConfiguration.WriteTo.Console(new JsonFormatter(renderMessage: false)),
            writeToProviders: true);
        // The matching AddCheck registrations live in Infrastructure; the Api must not duplicate either call.
        builder.Services.AddHealthChecks();
        return builder;
    }

    public static IServiceCollection AddLamuFlixOptions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        BindAndValidate<LibraryOptions>(services, LibraryOptions.SectionName);
        BindAndValidate<PlaybackOptions>(services, PlaybackOptions.SectionName);
        BindAndValidate<OmdbOptions>(services, OmdbOptions.SectionName);
        BindAndValidate<MetadataProviderResilienceOptions>(services, MetadataProviderResilienceOptions.SectionName);
        BindAndValidate<RabbitMqOptions>(services, RabbitMqOptions.SectionName);
        BindAndValidate<EnrichmentOptions>(services, EnrichmentOptions.SectionName);
        BindAndValidate<FeatureOptions>(services, FeatureOptions.SectionName);
        return services;
    }

    private static void BindAndValidate<TOptions>(IServiceCollection services, string sectionName)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private sealed class ServiceDefaultsMarker
    {
    }
}
