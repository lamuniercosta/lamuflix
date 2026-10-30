using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddLamuFlixPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' not configured. Set 'ConnectionStrings:{ConnectionStringName}' via user secrets " +
                "or the ConnectionStrings__DefaultConnection environment variable.");
        }

        services.AddDbContext<LamuFlixDbContext>(options => options.UseNpgsql(connectionString, o => o.UseVector()));
        services.AddHealthChecks().AddDbContextCheck<LamuFlixDbContext>("postgres", tags: [HealthCheckTags.Ready]);
        services.AddHybridCache();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<GenreWeightsCache>();
        services.TryAddScoped<IMovieCatalog, EfMovieCatalog>();
        services.TryAddScoped<GenreWeightsCalculator>();
        services.AddOptions<TasteProfileCacheOptions>()
            .BindConfiguration(TasteProfileCacheOptions.SectionName)
            .Validate(
                options => options.Expiration > TimeSpan.Zero,
                $"{TasteProfileCacheOptions.SectionName}:{nameof(TasteProfileCacheOptions.Expiration)} must be greater than zero.")
            .ValidateOnStart();
        services.AddOptions<HybridCacheOptions>()
            .Configure<IOptions<TasteProfileCacheOptions>>((hybrid, tasteProfile) =>
            {
                hybrid.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = tasteProfile.Value.Expiration,
                    LocalCacheExpiration = tasteProfile.Value.Expiration,
                };
            });
        services.AddOptions<EnrichmentOptions>()
            .Validate(
                options => options.ClaimLease > TimeSpan.Zero,
                "Enrichment:ClaimLease must be explicitly configured and greater than zero when production persistence is registered")
            .ValidateOnStart();
        services.AddScoped<IMovieRepository, EfMovieRepository>();
        services.AddScoped<IEnrichmentOutbox, EfEnrichmentOutbox>();
        services.AddScoped<IPlaybackHistory, EfPlaybackHistory>();
        services.AddScoped<IRecommendationCandidateSource, EfRecommendationCandidateSource>();
        services.AddScoped<EfRecommendationProfileSource>();
        services.AddSingleton<HybridCachedRecommendationProfileSource>();
        services.AddSingleton<IRecommendationProfileSource>(
            provider => provider.GetRequiredService<HybridCachedRecommendationProfileSource>());
        services.AddScoped<IMovieFeaturesRepository, EfMovieFeaturesRepository>();
        services.AddScoped<IOmdbRequestGate, EfMetadataRequestGate>();

        return services;
    }
}
