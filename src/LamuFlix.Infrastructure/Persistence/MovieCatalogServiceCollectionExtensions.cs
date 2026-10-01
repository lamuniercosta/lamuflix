using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace LamuFlix.Infrastructure.Persistence;

public static class MovieCatalogServiceCollectionExtensions
{
    public static IServiceCollection AddMovieCatalog(this IServiceCollection services)
    {
        services.AddScoped<IMovieCatalog, EfMovieCatalog>();
        return services;
    }
}