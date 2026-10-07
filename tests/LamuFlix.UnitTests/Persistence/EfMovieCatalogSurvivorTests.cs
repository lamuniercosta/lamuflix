using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;

namespace LamuFlix.UnitTests.Persistence;

[Collection(nameof(CatalogMutationCollection))]
public sealed class EfMovieCatalogSurvivorTests(PostgresFixture fixture)
{
    [Fact]
    public async Task BrowseAsync_SecondPageOfTitleSort_SkipsFirstPageInAscendingOrder()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        context.Movies.AddRange(
            Movie("Delta", "delta"),
            Movie("Alpha", "alpha"),
            Movie("Charlie", "charlie"),
            Movie("Bravo", "bravo"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var page = await catalog.BrowseAsync(Query(2, 2), TestContext.Current.CancellationToken);

        // assert
        page.TotalCount.ShouldBe(4);
        page.Items.Select(item => item.Title).ShouldBe(["Charlie", "Delta"]);
    }

    [Fact]
    public async Task GetDetailsAsync_TwoMovies_ReturnsTheRequestedMovie()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        context.Movies.AddRange(Movie("First", "first"), Movie("Second", "second"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var requested = context.Movies.Single(movie => movie.Title == "Second").Id;
        requested.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(requested, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Id.ShouldBe(requested);
        details.Title.ShouldBe("Second");
    }

    [Fact]
    public async Task GetDetailsAsync_MissingId_ReturnsNull()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(new MovieId(1), TestContext.Current.CancellationToken);

        // assert
        details.ShouldBeNull();
    }

    private static MovieQuery Query(int number, int size) => new()
    {
        Sort = MovieSort.Title,
        Direction = SortDirection.Ascending,
        Page = new Page(number, size)
    };

    private static MovieRecord Movie(string title, string suffix) => new()
    {
        Title = title,
        LibraryPath = new LibraryPath($"C:/library/{suffix}.mkv"),
        Format = new MediaFormat("mkv"),
        Status = EnrichmentStatus.Pending
    };
}

[CollectionDefinition(nameof(CatalogMutationCollection))]
public sealed class CatalogMutationCollection : ICollectionFixture<PostgresFixture>;
