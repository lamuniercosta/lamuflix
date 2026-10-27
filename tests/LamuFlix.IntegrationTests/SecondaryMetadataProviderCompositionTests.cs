using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(SecondaryMetadataProviderCollection.Name)]
public sealed class SecondaryMetadataProviderCompositionTests(SecondaryMetadataProviderProbe probe)
    : IClassFixture<SecondaryMetadataProviderProbe>, IAsyncLifetime
{
    private const string UnauthorizedAdvice = "TMDb API key";

    public static TheoryData<bool> EnabledValues => new() { true, false };

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [MemberData(nameof(EnabledValues))]
    public void AddMetadataProvider_UnderEitherEnabledValue_ResolvesTheConcreteProviderAndKeepsOmdbOnThePort(
        bool enabled)
    {
        // arrange
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = enabled ? "true" : "false",
        });

        // act
        var concrete = services.GetRequiredService<TmdbMetadataProvider>();
        var port = services.GetRequiredService<IMetadataProvider>();

        // assert
        concrete.ShouldNotBeNull();
        port.ShouldBeOfType<OmdbMetadataProvider>();
    }

    [Theory]
    [MemberData(nameof(EnabledValues))]
    public void AddMetadataProvider_UnderEitherEnabledValue_RegistersTheSecondaryCheckUntagged(bool enabled)
    {
        // arrange
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = enabled ? "true" : "false",
        });

        // act
        var registrations = services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        // assert
        registrations.Single(registration => registration.Name == SecondaryMetadataProviderProbe.CheckName)
            .Tags.ShouldBeEmpty();
    }

    [Fact]
    public void AddMetadataProvider_DoesNotInvokeAddLamuFlixOptions()
    {
        // arrange
        var services = probe.NewServicesWithoutOptions();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(ValidConfiguration())
            .Build());

        // act
        services.AddMetadataProvider();

        // assert
        services.Any(descriptor => descriptor.ServiceType == typeof(IValidateOptions<TmdbOptions>)).ShouldBeFalse();
        services.Any(descriptor => descriptor.ServiceType == typeof(IValidateOptions<LibraryOptions>)).ShouldBeFalse();

        services.AddLamuFlixOptions();
        services.Any(descriptor => descriptor.ServiceType == typeof(IValidateOptions<TmdbOptions>)).ShouldBeTrue();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<TmdbMetadataProvider>().ShouldNotBeNull();
    }

    private static Dictionary<string, string?> ValidConfiguration() =>
        new(StringComparer.Ordinal)
        {
            [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = "C:/library",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = "not-a-real-key",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://example.invalid/",
            [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
            [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}"] = "00:05:00",
            [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.SweepInterval)}"] = "00:01:00",
            [$"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}"] = "false",
        };

    [Fact]
    public async Task SecondaryHealth_BeforeAnyLookupWithEnabledAndKey_IsHealthy()
    {
        // arrange
        var services = probe.BuildServices();

        // act
        var entry = await CheckAsync(services, SecondaryMetadataProviderProbe.CheckName);

        // assert
        entry.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task SecondaryHealth_DisabledWithKeyAndNoFailure_IsHealthy()
    {
        // arrange
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = "false",
        });

        // act
        var entry = await CheckAsync(services, SecondaryMetadataProviderProbe.CheckName);

        // assert
        entry.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Theory]
    [MemberData(nameof(EnabledValues))]
    public async Task SecondaryHealth_WithBlankApiKey_IsUnhealthyWithConfigurationAdvice(bool enabled)
    {
        // arrange
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = enabled ? "true" : "false",
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.ApiKey)}"] = string.Empty,
        });

        // act
        var entry = await CheckAsync(services, SecondaryMetadataProviderProbe.CheckName);

        // assert
        entry.Status.ShouldBe(HealthStatus.Unhealthy);
        entry.Description.ShouldNotBeNull().ShouldContain(UnauthorizedAdvice);
    }

    [Fact]
    public async Task SecondaryHealth_AfterFound_IsHealthy()
    {
        // arrange
        probe.StubFoundLookup();
        var services = probe.BuildServices();
        await FindAsync(services);

        // act
        var entry = await CheckAsync(services, SecondaryMetadataProviderProbe.CheckName);

        // assert
        entry.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task SecondaryHealth_AfterNotFound_IsHealthy()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "empty.json"));
        var services = probe.BuildServices();
        await FindAsync(services);

        // act
        var entry = await CheckAsync(services, SecondaryMetadataProviderProbe.CheckName);

        // assert
        entry.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task SecondaryHealth_AfterRateLimited_IsDegraded()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 429);
        var services = probe.BuildServices();
        await FindAsync(services);

        // act
        var entry = await CheckAsync(services, SecondaryMetadataProviderProbe.CheckName);

        // assert
        entry.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task SecondaryHealth_AfterUnauthorized_IsUnhealthyAndLeavesThePrimaryCheckHealthy()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 401);
        var services = probe.BuildServices();
        await FindAsync(services);

        // act
        var secondary = await CheckAsync(services, SecondaryMetadataProviderProbe.CheckName);
        var primary = await CheckAsync(services, SecondaryMetadataProviderProbe.PrimaryCheckName);

        // assert
        secondary.Status.ShouldBe(HealthStatus.Unhealthy);
        secondary.Description.ShouldNotBeNull().ShouldContain(UnauthorizedAdvice);
        primary.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task SecondaryHealth_AfterUnauthorizedThenFound_RecoversToHealthy()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 401);
        var services = probe.BuildServices();
        await FindAsync(services);

        probe.Reset();
        probe.StubFoundLookup();
        await FindAsync(services);

        // act
        var entry = await CheckAsync(services, SecondaryMetadataProviderProbe.CheckName);

        // assert
        entry.Status.ShouldBe(HealthStatus.Healthy);
    }

    private static Task FindAsync(IServiceProvider services) =>
        services
            .GetRequiredService<TmdbMetadataProvider>()
            .FindAsync(new MetadataLookup("The Matrix", null), TestContext.Current.CancellationToken);

    private static async Task<HealthReportEntry> CheckAsync(IServiceProvider services, string name) =>
        (await services
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registration => registration.Name == name, TestContext.Current.CancellationToken))
        .Entries[name];
}
