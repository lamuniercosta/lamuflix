using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LamuFlix.Infrastructure.Persistence;

public static class MovieCatalogServiceCollectionExtensions
{
    public static IServiceCollection AddMovieCatalog(this IServiceCollection services)
    {
        services.TryAddSingleton<GenreWeightsCache>();
        services.TryAddScoped<GenreWeightsCalculator>();
        services.AddScoped<IMovieCatalog, EfMovieCatalog>();
        return services;
    }
}
