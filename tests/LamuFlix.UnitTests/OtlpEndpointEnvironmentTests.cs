using System;
using System.Collections.Generic;
using LamuFlix.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace LamuFlix.UnitTests;

[CollectionDefinition(nameof(OtlpEndpointEnvironmentCollection), DisableParallelization = true)]
public sealed class OtlpEndpointEnvironmentCollection;

[Collection(nameof(OtlpEndpointEnvironmentCollection))]
public sealed class OtlpEndpointEnvironmentTests
{
    private const string PlaceholderConnectionString =
        "Host=localhost;Database=unused;Username=unused;Password=unused";

    private const string EndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private static readonly Uri DefaultGrpcEndpoint = new("http://localhost:4317");
    private static readonly Uri DefaultHttpEndpoint = new("http://localhost:4318");
    private static readonly Uri OverrideEndpoint = new("http://otlp.test.invalid:4318/");

    [Fact]
    public void AddServiceDefaults_UnsetEndpointVariable_ResolvesDefaultOtlpEndpoint()
    {
        // arrange
        var previous = Environment.GetEnvironmentVariable(EndpointVariable);
        try
        {
            Environment.SetEnvironmentVariable(EndpointVariable, null);

            // act
            using var app = BuildProductionDefaults();
            var options = ResolveOtlpExporterOptions(app.Services);

            // assert
            options.Endpoint.ShouldBe(ExpectedDefaultEndpoint(options));
        }
        finally
        {
            Environment.SetEnvironmentVariable(EndpointVariable, previous);
        }
    }

    [Fact]
    public void AddServiceDefaults_OtlpExporterOptions_AreSharedAcrossAllThreeProviders()
    {
        // arrange
        var previous = Environment.GetEnvironmentVariable(EndpointVariable);
        try
        {
            Environment.SetEnvironmentVariable(EndpointVariable, null);

            // act
            using var app = BuildProductionDefaults();
            app.Services.GetService<TracerProvider>().ShouldNotBeNull();
            app.Services.GetService<MeterProvider>().ShouldNotBeNull();
            app.Services.GetService<LoggerProvider>().ShouldNotBeNull();
            var factory = RequireOptionsFactory(app.Services);
            var traces = factory.Create(Microsoft.Extensions.Options.Options.DefaultName);
            var metrics = factory.Create(Microsoft.Extensions.Options.Options.DefaultName);
            var logs = factory.Create(Microsoft.Extensions.Options.Options.DefaultName);

            // assert
            traces.Endpoint.ShouldBe(metrics.Endpoint);
            traces.Endpoint.ShouldBe(logs.Endpoint);
            traces.Protocol.ShouldBe(metrics.Protocol);
            traces.Protocol.ShouldBe(logs.Protocol);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EndpointVariable, previous);
        }
    }

    [Fact]
    public void AddServiceDefaults_StandardEndpointVariableSet_WinsOverDefault()
    {
        // arrange
        var previous = Environment.GetEnvironmentVariable(EndpointVariable);
        try
        {
            Environment.SetEnvironmentVariable(EndpointVariable, OverrideEndpoint.AbsoluteUri);

            // act
            using var app = BuildProductionDefaults();
            var options = ResolveOtlpExporterOptions(app.Services);

            // assert
            options.Endpoint.ShouldBe(OverrideEndpoint);
            options.Endpoint.ShouldNotBe(ExpectedDefaultEndpoint(options));
        }
        finally
        {
            Environment.SetEnvironmentVariable(EndpointVariable, previous);
        }
    }

    private static OtlpExporterOptions ResolveOtlpExporterOptions(IServiceProvider services)
    {
        var direct = services.GetService<OtlpExporterOptions>();
        if (direct is not null)
        {
            return direct;
        }

        return RequireOptionsFactory(services).Create(Microsoft.Extensions.Options.Options.DefaultName);
    }

    private static IOptionsFactory<OtlpExporterOptions> RequireOptionsFactory(IServiceProvider services)
    {
        var factory = services.GetService<IOptionsFactory<OtlpExporterOptions>>();
        if (factory is null)
        {
            throw new InvalidOperationException(
                "OtlpExporterOptions did not resolve from the container that production AddServiceDefaults() built.");
        }

        return factory;
    }

    private static Uri ExpectedDefaultEndpoint(OtlpExporterOptions options) =>
        options.Protocol == OtlpExportProtocol.HttpProtobuf
            ? DefaultHttpEndpoint
            : DefaultGrpcEndpoint;

    private static WebApplication BuildProductionDefaults()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = PlaceholderConnectionString,
        });
        builder.AddServiceDefaults();
        return builder.Build();
    }
}
