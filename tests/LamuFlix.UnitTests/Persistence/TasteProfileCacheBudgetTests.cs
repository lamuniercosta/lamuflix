using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Persistence;

public sealed class TasteProfileCacheBudgetTests
{
    [Fact]
    public void LiveHomeShapedProfile_SerializesStrictlyBelowTheWarningBudget()
    {
        // arrange
        var serializer = new CachedProfileSerializer();
        var warningThreshold = WarningThreshold();

        // act
        var bytes = SerializedBytes(serializer, 4);

        // assert
        ((double)bytes).ShouldBeLessThan(warningThreshold);
        bytes.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TicketBaselineFixture_SerializesStrictlyBelowTheWarningBudget()
    {
        // arrange
        var serializer = new CachedProfileSerializer();
        var warningThreshold = WarningThreshold();

        // act
        var bytes = SerializedBytes(serializer, 145);

        // assert
        ((double)bytes).ShouldBeLessThan(warningThreshold);
    }

    [Fact]
    public void WarningBandFixture_SerializesInsideTheNearLimitBandBelowTheCeiling()
    {
        // arrange
        var serializer = new CachedProfileSerializer();
        var ceiling = Ceiling();
        var warningThreshold = WarningThreshold();

        // act
        var bytes = SerializedBytes(serializer, 300);

        // assert
        ((double)bytes).ShouldBeGreaterThanOrEqualTo(warningThreshold);
        bytes.ShouldBeLessThan(ceiling);
    }

    [Fact]
    public void SerializedBytes_GrowMonotonicallyWithLikedSourceCount()
    {
        // arrange
        var serializer = new CachedProfileSerializer();

        // act
        var home = SerializedBytes(serializer, 4);
        var baseline = SerializedBytes(serializer, 145);
        var warningBand = SerializedBytes(serializer, 300);

        // assert
        home.ShouldBeLessThan(baseline);
        baseline.ShouldBeLessThan(warningBand);
    }

    [Fact]
    public void CompactFixture_CarriesNoPerSourcePlotVectorInTheCachedShape()
    {
        // arrange
        var profile = TasteProfileCacheBudgetFixture.Profile(300);

        // act
        var members = typeof(LamuFlix.Core.Domain.LikedReviewSource).GetProperties().Select(property => property.Name).ToArray();

        // assert
        members.ShouldNotContain(name => name.Contains("Plot", StringComparison.OrdinalIgnoreCase));
        members.ShouldNotContain(name => name.Contains("Embedding", StringComparison.OrdinalIgnoreCase));
        profile.LikedSources.Length.ShouldBe(300);
    }

    [Fact]
    public void EffectiveCeiling_ComesFromResolvedHybridCacheOptions()
    {
        // arrange
        using var provider = RegistrationTestsSupport.Compose().BuildServiceProvider();

        // act
        var ceiling = provider.GetRequiredService<IOptions<HybridCacheOptions>>().Value.MaximumPayloadBytes;

        // assert
        ceiling.ShouldBeGreaterThan(0);
        ceiling.ShouldBe(Ceiling());
    }

    [Fact]
    public void ConstructionObservation_UsesTheRegisteredSingletonSerializerOnTheSameEnvelope()
    {
        // arrange
        using var provider = RegistrationTestsSupport.Compose().BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var serializer = provider.GetRequiredService<CachedProfileSerializer>();
        var adapter = provider.GetRequiredService<HybridCachedRecommendationProfileSource>();
        var envelope = TasteProfileCacheBudgetFixture.Envelope(300);
        var writer = new ArrayBufferWriter<byte>();
        serializer.Serialize(envelope, writer);

        // act
        var observed = (long)writer.WrittenCount;

        // assert
        ReferenceEquals(adapter.Serializer, serializer).ShouldBeTrue();
        observed.ShouldBe(serializer.LastSerializedBytes);
        ((double)observed).ShouldBeInRange(
            WarningThreshold(),
            Ceiling() - 1);
    }

    [Fact]
    public async Task RealAdmission_AtWarningBandScale_ReusesWithoutReSerializing()
    {
        // arrange
        await using var provider = RegistrationTestsSupport.Compose().BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var serializer = provider.GetRequiredService<CachedProfileSerializer>();
        var cache = provider.GetRequiredService<HybridCache>();
        var envelope = TasteProfileCacheBudgetFixture.Envelope(300);
        var ct = TestContext.Current.CancellationToken;

        // act
        var first = await cache.GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.BudgetProbe",
            _ => ValueTask.FromResult(envelope),
            cancellationToken: ct);
        var bytesAfterFirst = serializer.LastSerializedBytes;
        var serializeCountAfterFirst = serializer.SerializeCount;
        var second = await cache.GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.BudgetProbe",
            _ => ValueTask.FromResult(envelope),
            cancellationToken: ct);

        // assert
        serializeCountAfterFirst.ShouldBe(1);
        serializer.SerializeCount.ShouldBe(serializeCountAfterFirst);
        serializer.LastSerializedBytes.ShouldBe(bytesAfterFirst);
        ((long)bytesAfterFirst).ShouldBeLessThan(Ceiling());
        first.Generation.ShouldBe(second.Generation);
    }

    private static long Ceiling() => new HybridCacheOptions().MaximumPayloadBytes;

    private static double WarningThreshold() => Ceiling() * TasteProfileCacheOptions.DefaultWarningFraction;

    [Fact]
    public void WarningThreshold_DistinguishesAdjacentIntegerByteValues()
    {
        // arrange
        var threshold = WarningThreshold();

        // assert
        threshold.ShouldBe(838860.8);
        ((double)838860).ShouldBeLessThan(threshold);
        ((double)838861).ShouldBeGreaterThanOrEqualTo(threshold);
        ((double)(long)threshold).ShouldBeLessThan(threshold);
    }

    private static long SerializedBytes(CachedProfileSerializer serializer, int likedSourceCount)
    {
        var writer = new ArrayBufferWriter<byte>();
        serializer.Serialize(TasteProfileCacheBudgetFixture.Envelope(likedSourceCount), writer);
        return writer.WrittenCount;
    }

    private static class RegistrationTestsSupport
    {
        internal static ServiceCollection Compose()
        {
            var services = new ServiceCollection();
            services.AddSingleton(Configuration());
            services.AddLamuFlixOptions();
            services.AddLogging();
            services.AddSingleton(NSubstitute.Substitute.For<IEnrichmentStatusNotifier>());
            services.AddLamuFlixPersistence(Configuration());
            return services;
        }

        private static IConfiguration Configuration() =>
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
                [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = "C:/library",
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = "not-a-real-key",
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://example.invalid/",
                [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
                ["Enrichment:ClaimLease"] = "00:05:00",
                ["Enrichment:SweepInterval"] = "00:01:00",
                [$"{TasteProfileCacheOptions.SectionName}:{nameof(TasteProfileCacheOptions.Expiration)}"] = "00:05:00",
            }).Build();
    }
}
