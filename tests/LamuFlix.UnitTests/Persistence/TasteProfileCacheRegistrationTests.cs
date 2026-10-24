using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Persistence;

public sealed class TasteProfileCacheRegistrationTests
{
    [Fact]
    public void AddLamuFlixPersistence_RegistersProcessLocalHybridCacheSingleton()
    {
        // arrange
        var services = Compose(Configuration(Valid("00:05:00")));
        using var provider = services.BuildServiceProvider();

        // act
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<HybridCache>();
        var sameScope = firstScope.ServiceProvider.GetRequiredService<HybridCache>();
        var second = secondScope.ServiceProvider.GetRequiredService<HybridCache>();

        // assert
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(HybridCache));
        first.ShouldNotBeNull();
        ReferenceEquals(first, sameScope).ShouldBeTrue();
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    public void AddLamuFlixPersistence_BindsConfiguredExpiration()
    {
        // arrange
        using var provider = Compose(Configuration(Valid("00:07:00"))).BuildServiceProvider();

        // act
        var expiration = provider.GetRequiredService<IOptions<TasteProfileCacheOptions>>().Value.Expiration;

        // assert
        expiration.ShouldBe(TimeSpan.FromMinutes(7));
    }

    [Fact]
    public void AddLamuFlixPersistence_MatchesHybridExpirationsToConfiguredDuration()
    {
        // arrange
        using var provider = Compose(Configuration(Valid("00:07:00"))).BuildServiceProvider();

        // act
        var hybrid = provider.GetRequiredService<IOptions<HybridCacheOptions>>().Value;
        var entryOptions = hybrid.DefaultEntryOptions;

        // assert
        entryOptions.ShouldNotBeNull();
        entryOptions.Expiration.ShouldBe(TimeSpan.FromMinutes(7));
        entryOptions.LocalCacheExpiration.ShouldBe(TimeSpan.FromMinutes(7));
    }

    [Fact]
    public void AddLamuFlixPersistence_RegistersSingletonAdapterAsRecommendationProfileSource()
    {
        // arrange
        var services = Compose(Configuration(Valid("00:05:00")));
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        // act
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var root = provider.GetRequiredService<IRecommendationProfileSource>();
        var first = firstScope.ServiceProvider.GetRequiredService<IRecommendationProfileSource>();
        var second = secondScope.ServiceProvider.GetRequiredService<IRecommendationProfileSource>();

        // assert
        services.Where(descriptor => descriptor.ServiceType == typeof(IRecommendationProfileSource))
            .ShouldHaveSingleItem()
            .Lifetime.ShouldBe(ServiceLifetime.Singleton);
        root.ShouldBeOfType<HybridCachedRecommendationProfileSource>();
        ReferenceEquals(root, first).ShouldBeTrue();
        ReferenceEquals(root, second).ShouldBeTrue();
    }

    [Fact]
    public void AddLamuFlixPersistence_RegistersScopedUncachedProfileSource()
    {
        // arrange
        var services = Compose(Configuration(Valid("00:05:00")));
        using var provider = services.BuildServiceProvider();

        // act
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<EfRecommendationProfileSource>();
        var sameScope = firstScope.ServiceProvider.GetRequiredService<EfRecommendationProfileSource>();
        var second = secondScope.ServiceProvider.GetRequiredService<EfRecommendationProfileSource>();

        // assert
        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(EfRecommendationProfileSource)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        ReferenceEquals(first, sameScope).ShouldBeTrue();
        ReferenceEquals(first, second).ShouldBeFalse();
    }

    private static ServiceCollection Compose(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLamuFlixOptions();
        services.AddLogging();
        services.AddLamuFlixPersistence(configuration);
        return services;
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> Valid(string expiration) =>
        new()
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
            [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = "C:/library",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = "not-a-real-key",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://example.invalid/",
            [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
            ["Enrichment:ClaimLease"] = "00:05:00",
            ["Enrichment:SweepInterval"] = "00:01:00",
            [$"{TasteProfileCacheOptions.SectionName}:{nameof(TasteProfileCacheOptions.Expiration)}"] = expiration,
        };
}
