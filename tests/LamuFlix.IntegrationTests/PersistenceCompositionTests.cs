using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.ServiceDefaults;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class PersistenceCompositionTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ComposedRegistration_ResolvesScopedRepositoryAndOneContextPerScope()
    {
        // arrange
        var services = await ComposeAsync();
        await using var provider = services.BuildServiceProvider(Validating());

        // act
        await using var firstScope = provider.CreateAsyncScope();
        await using var secondScope = provider.CreateAsyncScope();
        var first = firstScope.ServiceProvider.GetRequiredService<IMovieRepository>();
        var sameScope = firstScope.ServiceProvider.GetRequiredService<IMovieRepository>();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();
        var sameContext = firstScope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();

        // assert
        first.ShouldBeOfType<EfMovieRepository>();
        ReferenceEquals(first, sameScope).ShouldBeTrue();
        ReferenceEquals(firstContext, sameContext).ShouldBeTrue();
        ReferenceEquals(firstContext, secondContext).ShouldBeFalse();
    }

    [Fact]
    public async Task ComposedRegistration_ValidConfiguration_PassesStartupValidation()
    {
        // arrange
        var services = await ComposeAsync();

        // act
        await using var provider = services.BuildServiceProvider(Validating());
        var validators = provider.GetServices<IStartupValidator>().ToArray();

        // assert
        validators.ShouldNotBeEmpty();
        foreach (var validator in validators)
        {
            validator.Validate();
        }
    }

    [Fact]
    public async Task ComposedRegistration_RoundTripsAMovieThroughTheResolvedRepository()
    {
        // arrange
        var services = await ComposeAsync();
        await using var provider = services.BuildServiceProvider(Validating());
        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMovieRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var id = await repository.NextIdentityAsync(cancellationToken);
        var movie = Movie.Create(
            id, "Composed Title", new LibraryPath("C:/library/composed.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await using var queryScope = provider.CreateAsyncScope();
        var queryRepository = queryScope.ServiceProvider.GetRequiredService<IMovieRepository>();
        var loaded = await queryRepository.GetAsync(id, cancellationToken);

        // assert
        loaded.ShouldNotBeNull();
        loaded.ShouldNotBeSameAs(movie);
        loaded.Id.ShouldBe(id);
        loaded.Title.ShouldBe(movie.Title);
        loaded.Path.ShouldBe(movie.Path);
    }

    private async Task<ServiceCollection> ComposeAsync()
    {
        await using var context = fixture.CreateMigratedContext();
        var connectionString = context.Database.GetConnectionString();
        connectionString.ShouldNotBeNullOrWhiteSpace();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = "C:/library",
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = "not-a-real-key",
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://example.invalid/",
                [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
                [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}"] = "00:05:00",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLamuFlixOptions();
        services.AddLogging();
        services.AddLamuFlixPersistence(configuration);
        return services;
    }

    private static ServiceProviderOptions Validating() =>
        new() { ValidateOnBuild = true, ValidateScopes = true };
}
