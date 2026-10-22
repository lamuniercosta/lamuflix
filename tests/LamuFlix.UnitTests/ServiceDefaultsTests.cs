using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.ServiceDefaults;
using LamuFlix.UnitTests.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog.Extensions.Logging;

namespace LamuFlix.UnitTests;

public sealed class ServiceDefaultsTests
{
    private const string PlaceholderConnectionString =
        "Host=localhost;Database=unused;Username=unused;Password=unused";

    private const int DeadEndpointExportTimeoutMilliseconds = 100;

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

    [Fact]
    public void AddServiceDefaults_NullBuilder_IsRejected()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null contract test.
        var exception = Should.Throw<ArgumentNullException>(() => WebApplicationBuilderExtensions(builder: null!));

        // assert
        exception.ParamName.ShouldBe("builder");

        static WebApplicationBuilder WebApplicationBuilderExtensions(WebApplicationBuilder builder)
        {
            builder.AddServiceDefaults();
            return builder;
        }
    }

    [Fact]
    public void MapDefaultEndpoints_NullApp_IsRejected()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null contract test.
        var exception = Should.Throw<ArgumentNullException>(() => ServiceDefaultsExtensions.MapDefaultEndpoints(null!));

        // assert
        exception.ParamName.ShouldBe("app");
    }

    [Fact]
    public void AddLamuFlixOptions_NullServices_IsRejected()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null contract test.
        var exception = Should.Throw<ArgumentNullException>(() => ServiceDefaultsExtensions.AddLamuFlixOptions(null!));

        // assert
        exception.ParamName.ShouldBe("services");
        exception.TargetSite.ShouldNotBeNull();
        exception.TargetSite.DeclaringType.ShouldBe(typeof(ArgumentNullException));
        (exception.StackTrace ?? string.Empty).ShouldNotContain("BindAndValidate");
    }

    [Fact]
    public void AddServiceDefaults_RegistersHealthCheckService()
    {
        // arrange
        var builder = BuilderWithConnectionString();

        // act
        builder.AddServiceDefaults();
        using var app = builder.Build();

        // assert
        app.Services.GetService<HealthCheckService>().ShouldNotBeNull();
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public void MapDefaultEndpoints_RegistersTheHealthProbe(string pattern)
    {
        // arrange
        var builder = BuilderWithConnectionString();
        builder.AddServiceDefaults();
        using var app = builder.Build();

        // act
        app.MapDefaultEndpoints();

        // assert
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ShouldContain(pattern);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public void MapDefaultEndpoints_HealthProbesAllowAnonymous(string pattern)
    {
        // arrange
        var builder = BuilderWithConnectionString();
        builder.AddServiceDefaults();
        using var app = builder.Build();

        // act
        app.MapDefaultEndpoints();

        // assert
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => string.Equals(e.RoutePattern.RawText, pattern, StringComparison.Ordinal));
        endpoint.Metadata.GetMetadata<IAllowAnonymous>().ShouldNotBeNull();
    }

    [Fact]
    public void MapDefaultEndpoints_ReturnsTheSameApp()
    {
        // arrange
        var builder = BuilderWithConnectionString();
        builder.AddServiceDefaults();
        using var app = builder.Build();

        // act
        var mapped = app.MapDefaultEndpoints();

        // assert
        mapped.ShouldBeSameAs(app);
    }

    [Fact]
    public void AddLamuFlixOptions_RegistersOllamaOptionsWithDefaults()
    {
        // arrange
        var services = OptionsServices();

        // act
        services.AddLamuFlixOptions();

        // assert
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IConfigureOptions<OllamaOptions>));
        OllamaOptions.SectionName.ShouldBe("Ollama");
    }

    [Fact]
    public void AddLamuFlixOptions_RegistersOllamaResilienceOptionsWithDefaults()
    {
        // arrange
        var services = OptionsServices();

        // act
        services.AddLamuFlixOptions();

        // assert
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IConfigureOptions<OllamaResilienceOptions>));
        OllamaResilienceOptions.SectionName.ShouldBe("Ollama:Resilience");
    }

    [Fact]
    public void AddLamuFlixOptions_RegistersLibraryOptionsBinding()
    {
        // arrange
        var services = OptionsServices();

        // act
        services.AddLamuFlixOptions();

        // assert
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IConfigureOptions<LibraryOptions>));
    }

    [Fact]
    public void AddLamuFlixOptions_RegistersMetadataProviderResilienceOptionsBinding()
    {
        // arrange
        var services = OptionsServices();

        // act
        services.AddLamuFlixOptions();

        // assert
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IConfigureOptions<MetadataProviderResilienceOptions>));
    }

    private static ServiceCollection OptionsServices()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        return services;
    }

    [Fact]
    public async Task MapDefaultEndpoints_LiveProbe_IgnoresRegisteredChecks()
    {
        // arrange
        var builder = BuilderWithConnectionString();
        builder.Services.AddHealthChecks().AddCheck("always-unhealthy", () => HealthCheckResult.Unhealthy("down"));
        builder.AddServiceDefaults();
        await using var app = builder.Build();
        app.MapDefaultEndpoints();

        // act
        var statusCode = await InvokeEndpointAsync(app, "/health/live");
        // assert
        statusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task MapDefaultEndpoints_ReadyProbe_ReflectsRegisteredChecksWithProblemDetails()
    {
        // arrange
        var builder = BuilderWithConnectionString();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks().AddCheck(
            "always-unhealthy",
            () => HealthCheckResult.Unhealthy("down"),
            tags: [HealthCheckTags.Ready]);
        builder.AddServiceDefaults();
        await using var app = builder.Build();
        app.MapDefaultEndpoints();

        // act
        var (statusCode, body) = await InvokeEndpointWithBodyAsync(app, "/health/ready");

        // assert
        statusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        body.ShouldContain("checks");
    }

    [Fact]
    public void AddServiceDefaults_ForwardsLogEventsToMicrosoftProviders()
    {
        // arrange
        var builder = BuilderWithConnectionString();
        builder.AddServiceDefaults();
        var recording = new RecordingLoggerProvider();
        builder.Logging.AddProvider(recording);
        using var app = builder.Build();

        // act
        app.Services.GetRequiredService<ILogger<ServiceDefaultsTests>>().LogInformation("Provider probe.");

        // assert
        recording.Entries.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddServiceDefaults_ConsoleJsonOmitsTheRenderedMessage()
    {
        // arrange
        var builder = BuilderWithConnectionString();
        builder.AddServiceDefaults();
        using var app = builder.Build();
        var logger = app.Services.GetRequiredService<ILogger<ServiceDefaultsTests>>();
        var writer = new StringWriter();
        var originalOut = Console.Out;

        // act
        Console.SetOut(writer);
        try
        {
            logger.LogInformation("Render probe {Value}.", 42);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        // assert
        var output = writer.ToString();
        output.ShouldContain("Render probe");
        output.ShouldNotContain("RenderedMessage");
    }

    private static async Task<int> InvokeEndpointAsync(WebApplication app, string pattern)
    {
        var (statusCode, _) = await InvokeEndpointWithBodyAsync(app, pattern);
        return statusCode;
    }

    private static async Task<(int StatusCode, string Body)> InvokeEndpointWithBodyAsync(WebApplication app, string pattern)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => string.Equals(e.RoutePattern.RawText, pattern, StringComparison.Ordinal));
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Response.Body = new MemoryStream();
        var handler = endpoint.RequestDelegate.ShouldNotBeNull();
        await handler(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return (context.Response.StatusCode, await reader.ReadToEndAsync());
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public IReadOnlyList<string> Entries => entries;

        private readonly List<string> entries = [];

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, entries);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(string categoryName, List<string> entries) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                entries.Add(categoryName + ":" + formatter(state, exception));
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();

                public void Dispose()
                {
                }
            }
        }
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

        // The OTLP exporters target localhost defaults with no collector listening. Disposing a host that
        // resolved MeterProvider flushes the metric reader to that dead endpoint, and with the default export
        // timeout the exporter's retries stall the test for seconds. A short timeout bounds that flush.
        builder.Services.PostConfigure<OtlpExporterOptions>(static options =>
            options.TimeoutMilliseconds = DeadEndpointExportTimeoutMilliseconds);
        return builder;
    }
}
