using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfMovieCatalogFacetsTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetGenresAsync_StoredGenres_ReturnsAllRowsSortedNameThenId()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var zoe = new GenreRecord { Name = "Zoe" };
        var drama = new GenreRecord { Name = "Drama" };
        var dramaAgain = new GenreRecord { Name = "Drama" };
        context.Genres.AddRange(zoe, drama, dramaAgain);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetGenresAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Select(facet => facet.Name).ShouldBe(["Drama", "Drama", "Zoe"]);
        facets.Select(facet => facet.Id).ShouldBe([drama.Id, dramaAgain.Id, zoe.Id]);
        facets.Select(facet => facet.Id).Distinct().Count().ShouldBe(facets.Count);
    }

    [Fact]
    public async Task GetGenresAsync_EmptyTable_ReturnsEmpty()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetGenresAsync(TestContext.Current.CancellationToken);

        // assert
        facets.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPeopleAsync_StoredActors_ReturnsActorRowsSortedNameThenId()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var zoe = new ActorRecord { Name = "Zoe" };
        var ada = new ActorRecord { Name = "Ada" };
        var adaAgain = new ActorRecord { Name = "Ada" };
        var director = new DirectorRecord { Name = "Ann" };
        context.Actors.AddRange(zoe, ada, adaAgain);
        context.Directors.Add(director);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetPeopleAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Select(facet => facet.Name).ShouldBe(["Ada", "Ada", "Zoe"]);
        facets.Select(facet => facet.Id).ShouldBe([ada.Id, adaAgain.Id, zoe.Id]);
        facets.Select(facet => facet.Id).Distinct().Count().ShouldBe(facets.Count);
    }

    [Fact]
    public async Task GetPeopleAsync_EmptyTable_ReturnsEmpty()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetPeopleAsync(TestContext.Current.CancellationToken);

        // assert
        facets.ShouldBeEmpty();
    }
}
