using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Enrichment;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace LamuFlix.IntegrationTests;

public sealed class ApiHostFactory : WebApplicationFactory<Program>
{
    private const string StubCheckName = "host-harness-stub";

    private readonly bool retainSweeper;

    public ApiHostFactory() : this(false)
    {
    }

    internal ApiHostFactory(bool retainSweeper)
    {
        this.retainSweeper = retainSweeper;
    }

    private static readonly KeyValuePair<string, string?>[] HostSettings =
    [
        new($"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}", "C:/library"),
        new($"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}", "not-a-real-key"),
        new($"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}", "https://example.invalid/"),
        new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}", "localhost"),
        new("ConnectionStrings:DefaultConnection", "Host=localhost;Port=5432;Database=lamuflix"),
        new($"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}", "00:00:10"),
        new($"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.SweepInterval)}", "00:01:00"),
        new($"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}", "false"),
        new("OTEL_EXPORTER_OTLP_TIMEOUT", "1000"),
    ];

    protected override IHost CreateHost(IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureHostConfiguration(static configuration => configuration.AddInMemoryCollection(HostSettings));
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
        builder.ConfigureTestServices(ReplaceHealthChecksWithStub);
        if (!retainSweeper)
        {
            builder.ConfigureTestServices(RemoveSweeper);
        }
    }

    private static void ReplaceHealthChecksWithStub(IServiceCollection services)
    {
        services.Configure<HealthCheckServiceOptions>(static options => options.Registrations.Clear());
        services.AddHealthChecks().AddCheck(StubCheckName, static () => HealthCheckResult.Healthy());
    }

    private static void RemoveSweeper(IServiceCollection services)
    {
        var registration = services.FirstOrDefault(descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(StrandedMovieSweeper));
        if (registration is not null)
        {
            services.Remove(registration);
        }
    }
}
