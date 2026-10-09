using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.ServiceDefaults;
using LamuFlix.UnitTests.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog.Extensions.Logging;

namespace LamuFlix.UnitTests;

public sealed class ServiceDefaultsTests
{
    private const string PlaceholderConnectionString =
        "Host=localhost;Database=unused;Username=unused;Password=unused";

    private static readonly DateTimeOffset FixedNow = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddServiceDefaults_NoPriorClock_ResolvesSystemClock()
    {
        // arrange
        var builder = BuilderWithConnectionString();

        // act
        builder.AddServiceDefaults();
        using var app = builder.Build();

        // assert
        app.Services.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddServiceDefaults_AfterPersistenceAndMetadataProvider_ResolvesSingleSystemClock()
    {
        // arrange
        var builder = BuilderWithConnectionString();
        builder.AddServiceDefaults();

        // act
        builder.Services.AddLamuFlixPersistence(builder.Configuration);
        builder.Services.AddMetadataProvider();
        using var app = builder.Build();

        // assert
        builder.Services.Count(descriptor => descriptor.ServiceType == typeof(TimeProvider)).ShouldBe(1);
        app.Services.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddServiceDefaults_PreRegisteredClock_PriorRegistrationWins()
    {
        // arrange
        var builder = BuilderWithConnectionString();
        var clock = new FixedTimeProvider(FixedNow);
        builder.Services.AddSingleton<TimeProvider>(clock);

        // act
        builder.AddServiceDefaults();
        using var app = builder.Build();

        // assert
        app.Services.GetRequiredService<TimeProvider>().ShouldBeSameAs(clock);
    }

    [Fact]
    public void AddServiceDefaults_SingleComposition_ResolvesSerilogLoggerFactory()
    {
        // arrange
        var builder = BuilderWithConnectionString();

        // act
        builder.AddServiceDefaults();
        using var app = builder.Build();

        // assert
        app.Services.GetRequiredService<ILoggerFactory>().ShouldBeOfType<SerilogLoggerFactory>();
    }

    [Fact]
    public void AddServiceDefaults_SingleComposition_DoesNotRegisterConsoleLoggerProvider()
    {
        // arrange
        var builder = BuilderWithConnectionString();

        // act
        builder.AddServiceDefaults();
        using var app = builder.Build();
        var providers = app.Services.GetServices<ILoggerProvider>();

        // assert
        providers.ShouldNotContain(static provider => provider is ConsoleLoggerProvider);
    }

    [Fact]
    public void AddServiceDefaults_SingleComposition_RegistersSerilogLoggerFactoryOnce()
    {
        // arrange
        var builder = BuilderWithConnectionString();

        // act
        builder.AddServiceDefaults();

        // assert
        SerilogLoggerFactoryRegistrations(builder).ShouldBe(1);
    }

    [Fact]
    public void AddServiceDefaults_DoubleComposition_RegistersEnrichmentValidatorOnce()
    {
        // arrange
        var builder = BuilderWithConnectionString();

        // act
        builder.AddServiceDefaults();
        builder.AddServiceDefaults();

        // assert
        builder.Services.Count(static descriptor => descriptor.ImplementationType == typeof(EnrichmentOptionsValidator))
            .ShouldBe(1);
    }

    [Fact]
    public void AddServiceDefaults_DoubleComposition_DoesNotDuplicateProviderRegistrations()
    {
        // arrange
        var single = BuilderWithConnectionString();
        single.AddServiceDefaults();
        var dual = BuilderWithConnectionString();

        // act
        dual.AddServiceDefaults();
        dual.AddServiceDefaults();

        // assert
        SerilogLoggerFactoryRegistrations(dual).ShouldBe(1);
        OpenTelemetryProviderRegistrations(dual).ShouldBe(OpenTelemetryProviderRegistrations(single));
    }

    [Fact]
    public void AddServiceDefaults_TracerExporterIsComposed()
    {
        // arrange
        var builder = BuilderWithConnectionString();

        // act
        builder.AddServiceDefaults();
        using var app = builder.Build();

        // assert
        app.Services.GetService<TracerProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void AddServiceDefaults_MeterExporterIsComposed()
    {
        // arrange
        var builder = BuilderWithConnectionString();

        // act
        builder.AddServiceDefaults();
        using var app = builder.Build();

        // assert
        app.Services.GetService<MeterProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void AddServiceDefaults_LoggerExporterIsComposed()
    {
        // arrange
        var builder = BuilderWithConnectionString();

        // act
        builder.AddServiceDefaults();
        using var app = builder.Build();

        // assert
        app.Services.GetService<LoggerProvider>().ShouldNotBeNull();
    }

    private static int SerilogLoggerFactoryRegistrations(WebApplicationBuilder builder) =>
        builder.Services.Count(descriptor =>
            descriptor.ServiceType == typeof(ILoggerFactory) && descriptor.ImplementationFactory is not null);

    private static int OpenTelemetryProviderRegistrations(WebApplicationBuilder builder) =>
        builder.Services.Count(descriptor =>
            descriptor.ServiceType == typeof(TracerProvider)
            || descriptor.ServiceType == typeof(MeterProvider)
            || descriptor.ServiceType == typeof(LoggerProvider));

    private static WebApplicationBuilder BuilderWithConnectionString()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = PlaceholderConnectionString,
        });
        return builder;
    }
}
