using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LamuFlix.UnitTests.Persistence;

public sealed class MovieCatalogRegistrationTests
{
    [Fact]
    public void AddMovieCatalog_RegistersSingletonCacheScopedCalculatorAndScopedCatalog()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddDbContext<LamuFlixDbContext>(options => options.UseNpgsql("Host=localhost;Database=unused"));
        services.AddMovieCatalog();

        // act
        var cache = services.Single(descriptor => descriptor.ServiceType == typeof(GenreWeightsCache));
        var calculator = services.Single(descriptor => descriptor.ServiceType == typeof(GenreWeightsCalculator));
        var catalog = services.Single(descriptor => descriptor.ServiceType == typeof(IMovieCatalog));

        // assert
        cache.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        calculator.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        catalog.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        catalog.ImplementationType.ShouldBe(typeof(EfMovieCatalog));
    }

    [Fact]
    public void AddMovieCatalog_ResolvesOneSharedCacheAcrossScopes()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddDbContext<LamuFlixDbContext>(options => options.UseNpgsql("Host=localhost;Database=unused"));
        services.AddMovieCatalog();
        using var provider = services.BuildServiceProvider();

        // act
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<GenreWeightsCache>();
        var second = secondScope.ServiceProvider.GetRequiredService<GenreWeightsCache>();

        // assert
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void AddMovieCatalog_ResolvesScopedCalculatorAndCatalog()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddDbContext<LamuFlixDbContext>(options => options.UseNpgsql("Host=localhost;Database=unused"));
        services.AddMovieCatalog();
        using var provider = services.BuildServiceProvider();

        // act
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstCalculator = firstScope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>();
        var sameScopeCalculator = firstScope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>();
        var secondCalculator = secondScope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>();
        var firstCatalog = firstScope.ServiceProvider.GetRequiredService<IMovieCatalog>();
        var secondCatalog = secondScope.ServiceProvider.GetRequiredService<IMovieCatalog>();

        // assert
        firstCalculator.ShouldBeSameAs(sameScopeCalculator);
        firstCalculator.ShouldNotBeSameAs(secondCalculator);
        firstCatalog.ShouldBeOfType<EfMovieCatalog>();
        firstCatalog.ShouldNotBeSameAs(secondCatalog);
    }
}
