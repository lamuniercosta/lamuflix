using System;
using System.Collections.Generic;
using System.Globalization;
using LamuFlix.Core.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace LamuFlix.IntegrationTests;

public sealed record ApiEndToEndHostSettings(
    string DefaultConnection,
    RabbitMqOptions RabbitMq,
    string OmdbBaseUrl,
    TimeProvider TimeProvider);

public sealed class ApiEndToEndFactory : WebApplicationFactory<Program>
{
    private const string StubCheckName = "host-harness-stub";
    private const string RootPath = "C:/library";
    private const string ClaimLease = "00:00:01";
    private const string LocalPlay = "false";
    private const string OtlpTimeout = "1000";

    private readonly ApiEndToEndHostSettings settings;
    private readonly Action<IServiceCollection>? configureTestServices;

    public ApiEndToEndFactory(ApiEndToEndHostSettings settings, Action<IServiceCollection>? configureTestServices = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.settings = settings;
        this.configureTestServices = configureTestServices;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(HostSettings()));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.ConfigureWebHost(builder);
        builder.UseDefaultServiceProvider(static options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });
        builder.ConfigureTestServices(ReplaceHealthChecksAndTimeProvider);
    }

    private void ReplaceHealthChecksAndTimeProvider(IServiceCollection services)
    {
        services.Configure<HealthCheckServiceOptions>(static options => options.Registrations.Clear());
        services.AddHealthChecks().AddCheck(StubCheckName, static () => HealthCheckResult.Healthy());
        services.RemoveAll<TimeProvider>();
        services.AddSingleton(settings.TimeProvider);
        configureTestServices?.Invoke(services);
    }

    private KeyValuePair<string, string?>[] HostSettings() =>
    [
        new("ConnectionStrings:DefaultConnection", settings.DefaultConnection),
        new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}", settings.RabbitMq.HostName),
        new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.Port)}", settings.RabbitMq.Port.ToString(CultureInfo.InvariantCulture)),
        new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.UserName)}", settings.RabbitMq.UserName),
        new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.Password)}", settings.RabbitMq.Password),
        new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.RetryDelay)}", settings.RabbitMq.RetryDelay.ToString("c", CultureInfo.InvariantCulture)),
        new($"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}", settings.OmdbBaseUrl),
        new($"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}", MetadataProviderProbe.SentinelApiKey),
        new($"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}", ClaimLease),
        new($"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}", LocalPlay),
        new($"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}", RootPath),
        new("OTEL_EXPORTER_OTLP_TIMEOUT", OtlpTimeout),
    ];
}
