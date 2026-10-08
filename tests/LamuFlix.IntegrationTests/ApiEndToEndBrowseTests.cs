using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence.Records;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(ApiEndToEndCollection))]
public sealed class ApiEndToEndBrowseTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture)
{
    private const string BrowseRoute = "/api/movies";
    private const string ApplicationJson = "application/json";

    [Fact]
    public async Task BrowseMovies_CombinedFilters_ReturnsOnlyTheRowMatchingEveryFilter()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var drama = new GenreRecord { Name = "Drama" };
        var comedy = new GenreRecord { Name = "Comedy" };
        var ada = new ActorRecord { Name = "Ada" };
        var bob = new ActorRecord { Name = "Bob" };
        var matching = Row("Matching Film", "browse-match", 110, 2000, EnrichmentStatus.Enriched, true);
        matching.Genres.Add(drama);
        matching.Actors.Add(ada);
        var wrongText = Row("Unrelated Film", "browse-text", 110, 2000, EnrichmentStatus.Enriched, true);
        wrongText.Genres.Add(drama);
        wrongText.Actors.Add(ada);
        var wrongGenre = Row("Matching Film", "browse-genre", 110, 2000, EnrichmentStatus.Enriched, true);
        wrongGenre.Genres.Add(comedy);
        wrongGenre.Actors.Add(ada);
        var wrongActor = Row("Matching Film", "browse-actor", 110, 2000, EnrichmentStatus.Enriched, true);
        wrongActor.Genres.Add(drama);
        wrongActor.Actors.Add(bob);
        var wrongRuntime = Row("Matching Film", "browse-runtime", 300, 2000, EnrichmentStatus.Enriched, true);
        wrongRuntime.Genres.Add(drama);
        wrongRuntime.Actors.Add(ada);
        var wrongYear = Row("Matching Film", "browse-year", 110, 1950, EnrichmentStatus.Enriched, true);
        wrongYear.Genres.Add(drama);
        wrongYear.Actors.Add(ada);
        var wrongStatus = Row("Matching Film", "browse-status", 110, 2000, EnrichmentStatus.Pending, true);
        wrongStatus.Genres.Add(drama);
        wrongStatus.Actors.Add(ada);
        var wrongWatchlist = Row("Matching Film", "browse-watchlist", 110, 2000, EnrichmentStatus.Enriched, false);
        wrongWatchlist.Genres.Add(drama);
        wrongWatchlist.Actors.Add(ada);
        await SeedManyAsync(matching, wrongText, wrongGenre, wrongActor, wrongRuntime, wrongYear, wrongStatus, wrongWatchlist);

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}?text=Matching&genreIds={drama.Id}&actorIds={ada.Id}&runtimeMin=100&runtimeMax=120" +
            "&runtimeIncludeUnknown=false&yearMin=1999&yearMax=2001&statuses=Enriched&inWatchlist=true" +
            "&sort=Title&direction=Ascending&page=1&pageSize=20",
            cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        ShouldCarryExactly(body, "items", "totalCount");
        body.GetProperty("totalCount").GetInt32().ShouldBe(1);
        var item = body.GetProperty("items").EnumerateArray().ToList().ShouldHaveSingleItem();
        ShouldCarryExactly(item, "id", "title");
        item.GetProperty("id").GetInt32().ShouldBe(matching.Id.ShouldNotBeNull().Value);
        item.GetProperty("title").GetString().ShouldBe("Matching Film");
    }

    [Fact]
    public async Task BrowseMovies_PageBoundaries_KeepExactTotalCountsAndStableRows()
    {
        // arrange
        var client = await StartHostAsync();
        var seeded = new[]
        {
            MovieCatalogSeed.Create("Paged Tie", "browse-page-one"),
            MovieCatalogSeed.Create("Paged Tie", "browse-page-two"),
            MovieCatalogSeed.Create("Paged Tie", "browse-page-three"),
            MovieCatalogSeed.Create("Paged Tie", "browse-page-four"),
            MovieCatalogSeed.Create("Paged Tie", "browse-page-five")
        };
        await SeedManyAsync(seeded);
        var expected = seeded.Select(movie => movie.Id.ShouldNotBeNull().Value).Order().ToArray();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var first = await ReadPageAsync(client, 1, cancellationToken);
        var second = await ReadPageAsync(client, 2, cancellationToken);
        var third = await ReadPageAsync(client, 3, cancellationToken);
        var pastEnd = await ReadPageAsync(client, 4, cancellationToken);
        var firstAgain = await ReadPageAsync(client, 1, cancellationToken);

        // assert
        TotalCount(first).ShouldBe(5);
        TotalCount(second).ShouldBe(5);
        TotalCount(third).ShouldBe(5);
        TotalCount(pastEnd).ShouldBe(5);
        TotalCount(firstAgain).ShouldBe(5);
        ShouldCarryExactly(first, "items", "totalCount");
        Ids(first).ShouldBe(expected[..2]);
        Ids(second).ShouldBe(expected[2..4]);
        Ids(third).ShouldBe(expected[4..]);
        Ids(pastEnd).ShouldBeEmpty();
        Ids(firstAgain).ShouldBe(Ids(first));
    }

    [Fact]
    public async Task BrowseMovies_YearSort_KeepsNullsLastAndTiesStable()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var alpha = MovieCatalogSeed.Create("Alpha", "browse-sort-alpha");
        alpha.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        var bravo = MovieCatalogSeed.Create("Bravo", "browse-sort-bravo");
        bravo.ReleaseYear = new ReleaseYear(2001, MovieCatalogSeed.FixedTime);
        var charlie = MovieCatalogSeed.Create("Charlie", "browse-sort-charlie");
        charlie.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        var zulu = MovieCatalogSeed.Create("Zulu", "browse-sort-zulu");
        await SeedManyAsync(alpha, bravo, charlie, zulu);

        // act
        var descending = await ReadSortedAsync(client, "Descending", cancellationToken);
        var descendingAgain = await ReadSortedAsync(client, "Descending", cancellationToken);
        var ascending = await ReadSortedAsync(client, "Ascending", cancellationToken);

        // assert
        TotalCount(descending).ShouldBe(4);
        TotalCount(descendingAgain).ShouldBe(4);
        TotalCount(ascending).ShouldBe(4);
        Items(descending).ShouldBe(new[]
        {
            Entry(bravo),
            Entry(alpha),
            Entry(charlie),
            Entry(zulu)
        });
        Items(descendingAgain).ShouldBe(Items(descending));
        Items(ascending).ShouldBe(new[]
        {
            Entry(alpha),
            Entry(charlie),
            Entry(bravo),
            Entry(zulu)
        });
    }

    private static MovieRecord Row(
        string title,
        string suffix,
        int minutes,
        int year,
        EnrichmentStatus status,
        bool watchlist)
    {
        var row = MovieCatalogSeed.Create(title, suffix);
        row.RuntimeMinutes = new Runtime(minutes);
        row.ReleaseYear = new ReleaseYear(year, MovieCatalogSeed.FixedTime);
        row.Status = status;
        row.IsInWatchlist = watchlist;
        return row;
    }

    private static async Task<JsonElement> ReadPageAsync(
        HttpClient client,
        int page,
        CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(
            $"{BrowseRoute}?sort=Title&direction=Ascending&page={page}&pageSize=2",
            cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    private static async Task<JsonElement> ReadSortedAsync(
        HttpClient client,
        string direction,
        CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(
            $"{BrowseRoute}?sort=Year&direction={direction}&page=1&pageSize=20",
            cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    private static int TotalCount(JsonElement body) => body.GetProperty("totalCount").GetInt32();

    private static int[] Ids(JsonElement body) =>
        [.. body.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetInt32())];

    private static List<(int Id, string? Title)> Items(JsonElement body) =>
        [.. body.GetProperty("items").EnumerateArray().Select(Item)];

    private static (int Id, string? Title) Item(JsonElement element) =>
        (element.GetProperty("id").GetInt32(), element.GetProperty("title").GetString());

    private static (int Id, string? Title) Entry(MovieRecord movie) =>
        (movie.Id.ShouldNotBeNull().Value, movie.Title);

    private static void ShouldCarryExactly(JsonElement element, params string[] names) =>
        element.EnumerateObject().Select(property => property.Name).ShouldBe(names, ignoreOrder: true);

    private async Task SeedManyAsync(params MovieRecord[] movies)
    {
        await using var context = Fixture.Postgres.CreateMigratedContext();
        context.Movies.AddRange(movies);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
