using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

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

    [Fact]
    public void AddLamuFlixPersistence_RegistersSingleSharedCachedProfileSerializer()
    {
        // arrange
        var services = Compose(Configuration(Valid("00:05:00")));
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        // act
        var first = provider.GetRequiredService<CachedProfileSerializer>();
        var second = provider.GetRequiredService<CachedProfileSerializer>();

        // assert
        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(CachedProfileSerializer)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    public void AddLamuFlixPersistence_InjectsThatSameSerializerInstanceIntoTheCachedSource()
    {
        // arrange
        using var provider = Compose(Configuration(Valid("00:05:00")))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        // act
        var registered = provider.GetRequiredService<CachedProfileSerializer>();
        var adapter = provider.GetRequiredService<HybridCachedRecommendationProfileSource>();

        // assert
        ReferenceEquals(adapter.Serializer, registered).ShouldBeTrue();
    }

    [Fact]
    public async Task HybridCacheAdmission_SerializesTheEnvelopeThroughTheRegisteredSingleton()
    {
        // arrange
        await using var provider = Compose(Configuration(Valid("00:05:00")))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var serializer = provider.GetRequiredService<CachedProfileSerializer>();

        // act
        var first = await provider.GetRequiredService<HybridCache>().GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.Probe",
            _ => ValueTask.FromResult(Envelope()),
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        serializer.SerializeCount.ShouldBe(1);
        serializer.DeserializeCount.ShouldBe(1);
        serializer.LastSerializedBytes.ShouldBeGreaterThan(0);
        first.Generation.ShouldBe(42);
        first.CreatedAt.ShouldBe(CreatedAt);
        first.Profile.GenrePreferences.Keys.ShouldBe(["Drama"]);
        first.Profile.PlaybackByMovieId.ShouldBeEmpty();
    }

    [Fact]
    public async Task HybridCacheAdmission_ByteObservationAndAdmissionAgreeOnTheSameEnvelope()
    {
        // arrange
        await using var provider = Compose(Configuration(Valid("00:05:00")))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var serializer = provider.GetRequiredService<CachedProfileSerializer>();
        var envelope = Envelope();

        // act
        await provider.GetRequiredService<HybridCache>().GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.Probe",
            _ => ValueTask.FromResult(envelope),
            cancellationToken: TestContext.Current.CancellationToken);
        var observed = serializer.LastSerializedBytes;

        var writer = new ArrayBufferWriter<byte>();
        serializer.Serialize(envelope, writer);

        // assert
        observed.ShouldBeGreaterThan(0);
        writer.WrittenCount.ShouldBe(observed);
        serializer.SerializeCount.ShouldBe(2);
    }

    [Fact]
    public async Task HybridCacheAdmission_SecondReadReusesTheAdmittedEntryWithoutReSerializing()
    {
        // arrange
        await using var provider = Compose(Configuration(Valid("00:05:00")))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var serializer = provider.GetRequiredService<CachedProfileSerializer>();
        var cache = provider.GetRequiredService<HybridCache>();
        var ct = TestContext.Current.CancellationToken;

        // act
        await cache.GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.Probe",
            _ => ValueTask.FromResult(Envelope()),
            cancellationToken: ct);
        await cache.GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.Probe",
            _ => ValueTask.FromResult(Envelope()),
            cancellationToken: ct);

        // assert
        serializer.SerializeCount.ShouldBe(1);
    }

    [Fact]
    public void AddLamuFlixPersistence_BindsConfiguredWarningFraction()
    {
        // arrange
        var configuration = Configuration(Valid("00:05:00"));
        configuration["TasteProfileCache:WarningFraction"] = "0.65";
        using var provider = Compose(configuration).BuildServiceProvider();

        // act
        var warningFraction = provider.GetRequiredService<IOptions<TasteProfileCacheOptions>>().Value.WarningFraction;

        // assert
        warningFraction.ShouldBe(0.65);
    }

    [Fact]
    public void AddLamuFlixPersistence_DefaultsWarningFractionToTheConstDefault()
    {
        // arrange
        using var provider = Compose(Configuration(Valid("00:05:00"))).BuildServiceProvider();

        // act
        var warningFraction = provider.GetRequiredService<IOptions<TasteProfileCacheOptions>>().Value.WarningFraction;

        // assert
        warningFraction.ShouldBe(TasteProfileCacheOptions.DefaultWarningFraction);
        TasteProfileCacheOptions.DefaultWarningFraction.ShouldBe(0.8);
    }

    [Fact]
    public void AddLamuFlixPersistence_InjectsTheWarningFractionIntoTheCachedSource()
    {
        // arrange
        var configuration = Configuration(Valid("00:05:00"));
        configuration["TasteProfileCache:WarningFraction"] = "0.65";
        using var provider = Compose(configuration).BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        // act
        var adapter = provider.GetRequiredService<HybridCachedRecommendationProfileSource>();

        // assert
        adapter.WarningFraction.ShouldBe(0.65);
    }

    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private static TasteProfile EnvelopeProfile()
    {
        return new TasteProfile(
            ImmutableDictionary.Create<string, double>(StringComparer.OrdinalIgnoreCase).Add("Drama", 0.5));
    }

    private static CachedProfile Envelope() => new(EnvelopeProfile(), 42, CreatedAt);

    private static ServiceCollection Compose(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLamuFlixOptions();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IEnrichmentStatusNotifier>());
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
            [$"{OmdbQuotaOptions.SectionName}:{nameof(OmdbQuotaOptions.DailyLimit)}"] = "25",
            [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
            ["Enrichment:ClaimLease"] = "00:05:00",
            ["Enrichment:SweepInterval"] = "00:01:00",
            [$"{TasteProfileCacheOptions.SectionName}:{nameof(TasteProfileCacheOptions.Expiration)}"] = expiration,
        };
}

