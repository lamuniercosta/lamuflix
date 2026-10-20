using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class MovieCatalogRegistrationTests
{
    [Fact]
    public void AddMovieCatalog_SeparateScopes_ResolvesScopedEfMovieCatalog()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddDbContext<LamuFlixDbContext>(options => options.UseNpgsql("Host=localhost;Database=unused", o => o.UseVector()));
        services.AddMovieCatalog();
        using var provider = services.BuildServiceProvider();

        // act
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<IMovieCatalog>();
        var sameScope = firstScope.ServiceProvider.GetRequiredService<IMovieCatalog>();
        var second = secondScope.ServiceProvider.GetRequiredService<IMovieCatalog>();

        // assert
        first.ShouldBeOfType<EfMovieCatalog>();
        ReferenceEquals(first, sameScope).ShouldBeTrue();
        ReferenceEquals(first, second).ShouldBeFalse();
    }

    [Fact]
    public void AddLamuFlixPersistence_Alone_RegistersOneSingletonGenreWeightsCache()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddLamuFlixPersistence(Configuration());
        using var provider = services.BuildServiceProvider();

        // act
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<GenreWeightsCache>();
        var sameScope = firstScope.ServiceProvider.GetRequiredService<GenreWeightsCache>();
        var second = secondScope.ServiceProvider.GetRequiredService<GenreWeightsCache>();

        // assert
        services.Count(descriptor => descriptor.ServiceType == typeof(GenreWeightsCache)).ShouldBe(1);
        ReferenceEquals(first, sameScope).ShouldBeTrue();
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    public void AddMovieCatalog_Alone_RegistersOneSingletonCacheAndScopedCalculator()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddDbContext<LamuFlixDbContext>(options => options.UseNpgsql("Host=localhost;Database=unused", o => o.UseVector()));
        services.AddMovieCatalog();
        using var provider = services.BuildServiceProvider();

        // act
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstCache = firstScope.ServiceProvider.GetRequiredService<GenreWeightsCache>();
        var secondCache = secondScope.ServiceProvider.GetRequiredService<GenreWeightsCache>();
        var firstCalculator = firstScope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>();
        var sameScope = firstScope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>();
        var secondCalculator = secondScope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>();

        // assert
        services.Count(descriptor => descriptor.ServiceType == typeof(GenreWeightsCache)).ShouldBe(1);
        ReferenceEquals(firstCache, secondCache).ShouldBeTrue();
        ReferenceEquals(firstCalculator, sameScope).ShouldBeTrue();
        ReferenceEquals(firstCalculator, secondCalculator).ShouldBeFalse();
        services.Single(descriptor => descriptor.ServiceType == typeof(GenreWeightsCalculator))
            .Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddLamuFlixPersistenceThenAddMovieCatalog_ResolvesOneSharedCache()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddLamuFlixPersistence(Configuration());
        services.AddMovieCatalog();
        using var provider = services.BuildServiceProvider();

        // act
        using var scope = provider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<GenreWeightsCache>();
        var calculator = scope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>();

        // assert
        services.Count(descriptor => descriptor.ServiceType == typeof(GenreWeightsCache)).ShouldBe(1);
        provider.GetRequiredService<GenreWeightsCache>().ShouldBeSameAs(cache);
        calculator.ShouldNotBeNull();
    }

    [Fact]
    public void AddMovieCatalogThenAddLamuFlixPersistence_ResolvesOneSharedCache()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMovieCatalog();
        services.AddLamuFlixPersistence(Configuration());
        using var provider = services.BuildServiceProvider();

        // act
        using var scope = provider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<GenreWeightsCache>();
        var calculator = scope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>();

        // assert
        services.Count(descriptor => descriptor.ServiceType == typeof(GenreWeightsCache)).ShouldBe(1);
        provider.GetRequiredService<GenreWeightsCache>().ShouldBeSameAs(cache);
        calculator.ShouldNotBeNull();
    }

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused",
                ["Enrichment:ClaimLease"] = "00:01:00",
            })
            .Build();
}
