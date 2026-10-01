using System;
using LamuFlix.Core.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LamuFlix.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddLamuFlixOptions();
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
}
