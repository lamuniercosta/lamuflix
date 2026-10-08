using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(ApiEndToEndCollection))]
public sealed class ApiEndToEndLibraryTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture)
{
    private const string ApplicationJson = "application/json";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string GenresRoute = "/api/genres";
    private const string PeopleRoute = "/api/people?role=actor";
    private const int UnknownId = 999999;

    [Fact]
    public async Task GetMovieDetails_KnownId_ReturnsTheFrozenScalarShape()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = MovieCatalogSeed.Create("Heat", "library-details");
        movie.MetadataTitle = "Heat";
        movie.Plot = "A thief steals the negatives.";
        movie.ReleaseYear = new ReleaseYear(1995, MovieCatalogSeed.FixedTime);
        movie.RuntimeMinutes = new Runtime(131);
        movie.ImdbRating = new ImdbRating(8.3m);
        movie.ImdbId = new ImdbId("tt0113277");
        await SeedAsync(movie);
        var id = movie.Id.ShouldNotBeNull().Value;

        // act
        var response = await client.GetAsync(DetailsRoute(id), cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        ShouldCarryExactly(body, "id", "title", "path", "format", "metadata");
        body.GetProperty("id").GetInt32().ShouldBe(id);
        body.GetProperty("title").GetString().ShouldBe("Heat");
        body.GetProperty("path").GetString().ShouldBe("C:/library/library-details.mkv");
        body.GetProperty("format").GetString().ShouldBe("mkv");
        var metadata = body.GetProperty("metadata");
        ShouldCarryExactly(metadata, "title", "synopsis", "releaseYear", "runtime", "imdbRating", "imdbId");
        metadata.GetProperty("title").GetString().ShouldBe("Heat");
        metadata.GetProperty("synopsis").GetString().ShouldBe("A thief steals the negatives.");
        metadata.GetProperty("releaseYear").GetInt32().ShouldBe(1995);
        metadata.GetProperty("runtime").GetInt32().ShouldBe(131);
        metadata.GetProperty("imdbRating").GetDecimal().ShouldBe(8.3m);
        metadata.GetProperty("imdbId").GetString().ShouldBe("tt0113277");
    }

    [Fact]
    public async Task GetMovieDetails_UnknownId_ReturnsNotFoundProblem()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(DetailsRoute(UnknownId), cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("title").GetString().ShouldBe("Not Found");
    }

    [Fact]
    public async Task GetGenres_SeededMetadata_ReturnsThePersistedIdNameArray()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var drama = new GenreRecord { Name = "Drama" };
        var comedy = new GenreRecord { Name = "Comedy" };
        var first = MovieCatalogSeed.Create("Alpha", "genres-alpha");
        first.Genres.Add(drama);
        first.Genres.Add(comedy);
        var second = MovieCatalogSeed.Create("Bravo", "genres-bravo");
        second.Genres.Add(drama);
        await SeedManyAsync(first, second);
        var persisted = await ReadPersistedGenresAsync();

        // act
        var response = await client.GetAsync(GenresRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        AssertIdNameShape(body);
        Facets(body).ShouldBe(persisted, ignoreOrder: true);
    }

    [Fact]
    public async Task GetPeople_ActorRole_ReturnsThePersistedIdNameArray()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var ada = new ActorRecord { Name = "Ada" };
        var bob = new ActorRecord { Name = "Bob" };
        var first = MovieCatalogSeed.Create("Alpha", "people-alpha");
        first.Actors.Add(ada);
        first.Actors.Add(bob);
        var second = MovieCatalogSeed.Create("Bravo", "people-bravo");
        second.Actors.Add(ada);
        await SeedManyAsync(first, second);
        var persisted = await ReadPersistedPeopleAsync();

        // act
        var response = await client.GetAsync(PeopleRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        AssertIdNameShape(body);
        Facets(body).ShouldBe(persisted, ignoreOrder: true);
    }

    [Fact]
    public async Task WatchlistMutations_PersistBothTransitionsWithEmptyBodies()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = await SeedAsync(MovieCatalogSeed.Create("Heat", "watchlist-heat"));
        var id = movie.Id.ShouldNotBeNull().Value;

        // act
        var added = await client.PostAsync(WatchlistRoute(id), null, cancellationToken);
        var addedBody = await added.Content.ReadAsStringAsync(cancellationToken);
        var persistedAfterAdd = await ReadPersistedWatchlistAsync(id, cancellationToken);
        var removed = await client.DeleteAsync(WatchlistRoute(id), cancellationToken);
        var removedBody = await removed.Content.ReadAsStringAsync(cancellationToken);
        var persistedAfterRemove = await ReadPersistedWatchlistAsync(id, cancellationToken);

        // assert
        added.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        addedBody.ShouldBe(string.Empty);
        persistedAfterAdd.ShouldBe(true);
        removed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        removedBody.ShouldBe(string.Empty);
        persistedAfterRemove.ShouldBe(false);
    }

    private static string DetailsRoute(int id) => $"/api/movies/{id}";

    private static string WatchlistRoute(int id) => $"/api/movies/{id}/watchlist";

    private static List<(int Id, string Name)> Facets(JsonElement body) =>
        [.. body.EnumerateArray().Select(Facet)];

    private static (int Id, string Name) Facet(JsonElement element)
    {
        var name = element.GetProperty("name").GetString().ShouldNotBeNull();
        return (element.GetProperty("id").GetInt32(), name);
    }

    private static void AssertIdNameShape(JsonElement body)
    {
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        foreach (var item in body.EnumerateArray())
        {
            ShouldCarryExactly(item, "id", "name");
            item.GetProperty("id").ValueKind.ShouldBe(JsonValueKind.Number);
            item.GetProperty("name").ValueKind.ShouldBe(JsonValueKind.String);
        }
    }

    private static void ShouldCarryExactly(JsonElement element, params string[] names) =>
        element.EnumerateObject().Select(property => property.Name).ShouldBe(names, ignoreOrder: true);

    private async Task<List<(int Id, string Name)>> ReadPersistedGenresAsync()
    {
        await using var context = Fixture.Postgres.CreateMigratedContext();
        var genres = await context.Genres.AsNoTracking()
            .OrderBy(genre => genre.Name)
            .ThenBy(genre => genre.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        return [.. genres.Select(genre => (genre.Id, genre.Name))];
    }

    private async Task<List<(int Id, string Name)>> ReadPersistedPeopleAsync()
    {
        await using var context = Fixture.Postgres.CreateMigratedContext();
        var actors = await context.Actors.AsNoTracking()
            .OrderBy(actor => actor.Name)
            .ThenBy(actor => actor.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        return [.. actors.Select(actor => (actor.Id, actor.Name))];
    }

    private async Task<bool?> ReadPersistedWatchlistAsync(int id, CancellationToken cancellationToken)
    {
        var movieId = new MovieId(id);
        await using var context = Fixture.Postgres.CreateMigratedContext();
        return await context.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == movieId)
            .Select(movie => (bool?)movie.IsInWatchlist)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task SeedManyAsync(params MovieRecord[] movies)
    {
        await using var context = Fixture.Postgres.CreateMigratedContext();
        context.Movies.AddRange(movies);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
