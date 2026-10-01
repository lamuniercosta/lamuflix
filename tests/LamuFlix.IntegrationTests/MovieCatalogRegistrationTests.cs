using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
        services.AddDbContext<LamuFlixDbContext>(options => options.UseNpgsql("Host=localhost;Database=unused"));
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
}