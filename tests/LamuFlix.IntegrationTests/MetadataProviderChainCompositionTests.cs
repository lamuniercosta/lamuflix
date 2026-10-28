using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(SecondaryMetadataProviderCollection.Name)]
public sealed class MetadataProviderChainCompositionTests(SecondaryMetadataProviderProbe probe)
    : IClassFixture<SecondaryMetadataProviderProbe>, IAsyncLifetime
{
    private const string PrimaryCheckName = "metadata-provider";
    private const string SecondaryCheckName = "metadata-provider-tmdb";
    private const string OmdbKeyAdvice = "Check the OMDb API key configuration.";
    private const string SecondaryPathPrefix = "/3";
    private const string PrimaryPath = "/";

    private const string OmdbFoundBody = """
        {
          "Response": "True",
          "Title": "Solaris",
          "Year": "1972",
          "Runtime": "167 min",
          "Plot": "A psychologist...",
          "imdbRating": "8.0",
          "imdbID": "tt0069293"
        }
        """;

    private const string OmdbNotFoundBody = """
        {
          "Response": "False",
          "Error": "Movie not found!"
        }
        """;

    public static TheoryData<bool> EnabledValues => new() { true, false };

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [MemberData(nameof(EnabledValues))]
    public void AddMetadataProvider_UnderEitherSecondaryEnabledValue_ResolvesBothKeysAndTheSoleChain(bool enabled)
    {
        // arrange
        var services = probe.BuildServices(PrimaryAndSecondary(enabled));

        // act
        var primary = services.GetRequiredKeyedService<IMetadataProvider>(TelemetryConstants.OmdbProviderName);
        var secondary = services.GetRequiredKeyedService<IMetadataProvider>(TelemetryConstants.TmdbProviderName);
        var port = services.GetRequiredService<IMetadataProvider>();

        // assert
        primary.ShouldBeOfType<OmdbMetadataProvider>();
        secondary.ShouldBeOfType<TmdbMetadataProvider>();
        port.ShouldBeOfType<ChainedMetadataProvider>();
    }

    [Fact]
    public void ProviderRegisteredBeforeRabbitMq_KeepsOneUnkeyedDescriptorAndActivatesTheEnrichmentConsumer()
    {
        // arrange
        var services = probe.NewServicesWithoutOptions();
        services.AddSingleton(Configuration(secondaryEnabled: false));
        services.AddSingleton<IMovieRepository, UnusedMovieRepository>();
        services.AddMetadataProvider();

        // act
        var unkeyedDescriptors = services.Count(descriptor =>
            descriptor.ServiceType == typeof(IMetadataProvider) && descriptor.ServiceKey is null);
        services.AddLamuFlixRabbitMq();

        // assert
        unkeyedDescriptors.ShouldBe(1);
        services.Any(descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(EnrichmentConsumer)).ShouldBeTrue();
        services.Any(descriptor =>
            descriptor.ServiceType == typeof(ProcessEnrichmentCommandHandler)).ShouldBeTrue();
    }

    [Fact]
    public void AddMetadataProvider_KeepsBothMetadataChecksUntaggedAndOutsideReadiness()
    {
        // arrange
        using var host = new ApiHostFactory(retainSweeper: false, retainHealthChecks: true);

        // act
        var registrations = host.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        var ready = registrations
            .Where(registration => registration.Tags.Contains(HealthCheckTags.Ready))
            .Select(registration => registration.Name)
            .ToArray();

        // assert
        registrations.Single(registration => registration.Name == PrimaryCheckName).Tags.ShouldBeEmpty();
        registrations.Single(registration => registration.Name == SecondaryCheckName).Tags.ShouldBeEmpty();
        ready.ShouldNotContain(PrimaryCheckName);
        ready.ShouldNotContain(SecondaryCheckName);
    }

    [Fact]
    public async Task FindAsync_WithSecondaryDisabled_NeverResolvesTheSecondaryAndReturnsThePrimaryResult()
    {
        // arrange
        var services = probe.NewServicesWithoutOptions();
        services.AddSingleton(Configuration(secondaryEnabled: false));
        services.AddLamuFlixOptions();
        services.AddMetadataProvider();
        var secondaryResolved = false;
        services.AddKeyedTransient<IMetadataProvider>(TelemetryConstants.TmdbProviderName, (_, _) =>
        {
            secondaryResolved = true;
            throw new InvalidOperationException("The disabled secondary provider must not be resolved.");
        });
        await using var provider = services.BuildServiceProvider();
        StubPrimary(200, OmdbFoundBody);

        // act
        var result = await FindChainAsync(provider, "Solaris");

        // assert
        Metadata(result).Title.ShouldBe("Solaris");
        secondaryResolved.ShouldBeFalse();
        SecondaryPaths().ShouldBeEmpty();
    }

    [Fact]
    public async Task FindAsync_WithSecondaryDisabled_MatchesTheDirectPrimaryOutcomes()
    {
        // arrange
        var services = probe.BuildServices(PrimaryAndSecondary(enabled: false));
        StubPrimary(200, OmdbFoundBody);

        // act
        var chained = await FindChainAsync(services, "Solaris");
        var direct = await services.GetRequiredService<OmdbMetadataProvider>()
            .FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        Metadata(chained).Title.ShouldBe(Metadata(direct).Title);
        Metadata(chained).ReleaseYear.ShouldBe(Metadata(direct).ReleaseYear);
        Metadata(chained).ImdbId.ShouldBe(Metadata(direct).ImdbId);

        probe.Reset();
        StubPrimary(200, OmdbNotFoundBody);
        var chainedMiss = await FindChainAsync(services, "Solaris");
        var directMiss = await services.GetRequiredService<OmdbMetadataProvider>()
            .FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        chainedMiss.ShouldBeOfType<MetadataLookupResult.NotFound>();
        directMiss.ShouldBeOfType<MetadataLookupResult.NotFound>();
        SecondaryPaths().ShouldBeEmpty();
    }

    [Fact]
    public async Task FindAsync_WhenThePrimaryMisses_CallsTheSecondaryAndReturnsItsFound()
    {
        // arrange
        var services = probe.BuildServices(PrimaryAndSecondary(enabled: true));
        StubPrimary(200, OmdbNotFoundBody);
        probe.StubFoundLookup();

        // act
        var result = await FindChainAsync(services, "The Matrix");

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
        SecondaryPaths().ShouldContain("/3/search/movie");
        SecondaryPaths().ShouldContain("/3/movie/603");
    }

    [Fact]
    public async Task FindAsync_WhenThePrimaryFinds_DoesNotCallTheSecondary()
    {
        // arrange
        var services = probe.BuildServices(PrimaryAndSecondary(enabled: true));
        StubPrimary(200, OmdbFoundBody);
        probe.StubFoundLookup();

        // act
        var result = await FindChainAsync(services, "Solaris");

        // assert
        Metadata(result).Title.ShouldBe("Solaris");
        SecondaryPaths().ShouldBeEmpty();
    }

    [Fact]
    public async Task FindAsync_WhenThePrimaryIsRateLimited_FallsBackToTheSecondary()
    {
        // arrange
        var services = probe.BuildServices(PrimaryAndSecondary(enabled: true));
        StubPrimary(429);
        probe.StubFoundLookup();

        // act
        var result = await FindChainAsync(services, "The Matrix");

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
        SecondaryPaths().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task FindAsync_WhenThePrimaryRejectsTheResponse_DoesNotCallTheSecondary()
    {
        // arrange
        var services = probe.BuildServices(PrimaryAndSecondary(enabled: true));
        StubPrimary(400);
        probe.StubFoundLookup();

        // act
        var result = await FindChainAsync(services, "The Matrix");

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        SecondaryPaths().ShouldBeEmpty();
    }

    [Fact]
    public async Task FindAsync_WhenThePrimaryIsRateLimited_EmitsTwoLookupSpansAndTwoProviderDurations()
    {
        // arrange
        _ = EnrichmentMetrics.EnrichmentDuration;
        var services = probe.BuildServices(PrimaryAndSecondary(enabled: true));
        StubPrimary(429);
        probe.StubFoundLookup();
        using var spans = new SecondaryActivityCapture(TelemetryConstants.ActivitySourceName);
        using var durations = new SecondaryMetricCapture(TelemetryConstants.EnrichmentDurationMetricName);
        using var ambient = new Activity("test.ambient").Start();

        // act
        var result = await FindChainAsync(services, "The Matrix");

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
        spans.LookupSpans(ambient.TraceId.ToHexString()).Count.ShouldBe(2);
        var measured = durations.Measurements
            .Where(measurement =>
                string.Equals(
                    measurement.Instrument.Name,
                    TelemetryConstants.EnrichmentDurationMetricName,
                    StringComparison.Ordinal))
            .Select(measurement => measurement.Tags.ShouldHaveSingleItem().Value?.ToString())
            .ToArray();
        measured.ShouldBe([TelemetryConstants.OmdbProviderName, TelemetryConstants.TmdbProviderName]);
    }

    [Fact]
    public async Task FindAsync_WhenThePrimaryIsRateLimitedAndTheSecondaryFinds_KeepsThePrimarySnapshotDegraded()
    {
        // arrange
        var services = probe.BuildServices(PrimaryAndSecondary(enabled: true));
        StubPrimary(429);
        probe.StubFoundLookup();

        // act
        var result = await FindChainAsync(services, "The Matrix");
        var primary = await CheckAsync(services, PrimaryCheckName);
        var secondary = await CheckAsync(services, SecondaryCheckName);

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
        primary.Status.ShouldBe(HealthStatus.Degraded);
        secondary.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task FindAsync_WhenThePrimaryIsUnauthorized_StopsWithoutTheSecondaryAndKeepsTheKeyAdvice()
    {
        // arrange
        var services = probe.BuildServices(PrimaryAndSecondary(enabled: true));
        StubPrimary(401);
        probe.StubFoundLookup();

        // act
        var result = await FindChainAsync(services, "The Matrix");
        var primary = await CheckAsync(services, PrimaryCheckName);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        SecondaryPaths().ShouldBeEmpty();
        primary.Status.ShouldBe(HealthStatus.Unhealthy);
        primary.Description.ShouldBe(OmdbKeyAdvice);
    }

    [Fact]
    public async Task FindAsync_WhenThePrimaryBreakerOpens_KeepsTheSecondaryBreakerIndependentAndCallable()
    {
        // arrange
        var overrides = PrimaryAndSecondary(enabled: true);
        overrides[SecondaryMetadataProviderProbe.Resilience(nameof(MetadataProviderResilienceOptions.MinimumThroughput))] =
            "2";
        var services = probe.BuildServices(overrides);
        StubPrimary(500);
        probe.StubFoundLookup();

        // act
        var opening = await FindChainAsync(services, "The Matrix");
        var primaryRequestsAfterOpening = PrimaryRequests();
        var open = await FindChainAsync(services, "The Matrix");
        var secondaryDirect = await services.GetRequiredService<TmdbMetadataProvider>()
            .FindAsync(new MetadataLookup("The Matrix", null), TestContext.Current.CancellationToken);

        // assert
        Metadata(opening).Title.ShouldBe("The Matrix");
        Metadata(open).Title.ShouldBe("The Matrix");
        PrimaryRequests().ShouldBe(primaryRequestsAfterOpening);
        SecondaryPaths().ShouldNotBeEmpty();
        Metadata(secondaryDirect).Title.ShouldBe("The Matrix");
    }

    private static MovieMetadata Metadata(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;

    private static EnrichmentFailureCategory Category(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category;

    private static Task<MetadataLookupResult> FindChainAsync(IServiceProvider services, string title) =>
        services
            .GetRequiredService<IMetadataProvider>()
            .FindAsync(new MetadataLookup(title, null), TestContext.Current.CancellationToken);

    private static async Task<HealthReportEntry> CheckAsync(IServiceProvider services, string name) =>
        (await services
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registration => registration.Name == name, TestContext.Current.CancellationToken))
        .Entries[name];

    private static Dictionary<string, string?> PrimaryAndSecondary(bool enabled) =>
        new(StringComparer.Ordinal)
        {
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = enabled ? "true" : "false",
        };

    private void StubPrimary(int statusCode, string body = "") =>
        probe.StubPath(PrimaryPath, statusCode, body);

    private IReadOnlyList<string> SecondaryPaths() =>
        [.. probe.RequestPaths.Where(path => path.StartsWith(SecondaryPathPrefix, StringComparison.Ordinal))];

    private int PrimaryRequests() =>
        probe.RequestPaths.Count(path => string.Equals(path, PrimaryPath, StringComparison.Ordinal));

    private IConfiguration Configuration(bool secondaryEnabled) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = "C:/library",
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] =
                    SecondaryMetadataProviderProbe.SentinelApiKey,
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = probe.Server.Urls[0],
                [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = secondaryEnabled ? "true" : "false",
                [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.ApiKey)}"] =
                    SecondaryMetadataProviderProbe.SentinelApiKey,
                [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.BaseUrl)}"] = probe.ApiBaseUrl,
                [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
                [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}"] = "00:05:00",
                [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.SweepInterval)}"] = "00:01:00",
                [$"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}"] = "false",
            })
            .Build();

    private sealed class UnusedMovieRepository : IMovieRepository
    {
        public Task<Movie?> GetAsync(MovieId id, CancellationToken ct) => Task.FromResult<Movie?>(null);

        public Task AddAsync(Movie movie, CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TryDeleteAsync(MovieId id, CancellationToken ct) => Task.FromResult(false);

        public Task<MovieId> NextIdentityAsync(CancellationToken ct) => Task.FromResult(new MovieId(1));

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct) => Task.FromResult(false);

        public Task<MovieId?> FindMovieIdByImdbIdAsync(ImdbId imdbId, CancellationToken ct) =>
            Task.FromResult<MovieId?>(null);

        public Task<IReadOnlyList<MovieId>> FindStrandedMovieIdsAsync(DateTimeOffset leaseCutoff, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MovieId>>([]);

        public Task<string?> GetPlotAsync(MovieId id, CancellationToken ct) => Task.FromResult<string?>(null);
    }
}
