using System;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        services.AddDbContext<LamuFlixDbContext>(options => options.UseNpgsql(connectionString));
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<EnrichmentOptions>()
            .Validate(
                options => options.ClaimLease > TimeSpan.Zero,
                "Enrichment:ClaimLease must be explicitly configured and greater than zero when production persistence is registered")
            .ValidateOnStart();
        services.AddScoped<IMovieRepository, EfMovieRepository>();

        return services;
    }
}
