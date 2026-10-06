using System;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
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
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddSource(
                    TelemetryConstants.ActivitySourceName,
                    TelemetryConstants.RabbitMqPublisherActivitySourceName,
                    TelemetryConstants.RabbitMqSubscriberActivitySourceName)
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(TelemetryConstants.ActivitySourceName)
                .AddOtlpExporter())
            .WithLogging(logging => logging.AddOtlpExporter());
        // The matching AddCheck registrations live in Infrastructure; the Api must not duplicate either call.
        builder.Services.AddHealthChecks();
        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();
        app.MapHealthChecks(
                "/health/ready",
                new HealthCheckOptions
                {
                    Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready),
                    ResponseWriter = HealthCheckResponseWriter.WriteAsync,
                })
            .AllowAnonymous();

        return app;
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
