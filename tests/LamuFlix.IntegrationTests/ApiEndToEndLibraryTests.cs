using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(ApiEndToEndCollection))]
public sealed class ApiEndToEndLibraryTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture)
{
    private const string ApplicationJson = "application/json";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string BrowseRoute = "/api/movies";
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
        movie.IsInWatchlist = true;
        await SeedAsync(movie);
        var id = movie.Id.ShouldNotBeNull().Value;

        // act
        var response = await client.GetAsync(DetailsRoute(id), cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        ShouldCarryExactly(body, "id", "title", "path", "format", "metadata", "isInWatchlist", "isWatched", "isNotInterested");
        body.GetProperty("isInWatchlist").GetBoolean().ShouldBeTrue();
        body.GetProperty("id").GetInt32().ShouldBe(id);
        body.GetProperty("title").GetString().ShouldBe("Heat");
        body.GetProperty("path").GetString().ShouldBe("C:/library/library-details.mkv");
        body.GetProperty("format").GetString().ShouldBe("mkv");
        var metadata = body.GetProperty("metadata");
        ShouldCarryExactly(metadata, "title", "synopsis", "releaseYear", "runtime", "imdbRating", "imdbId", "posterUrl", "rottenTomatoesRating", "metaScore", "directors", "actors");
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
        AssertGenreShape(body);
        GenreFacets(body).ShouldBe(persisted, ignoreOrder: true);
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

    public static TheoryData<bool, bool, bool> FeedbackCombinations => new()
    {
        { false, false, false },
        { false, false, true },
        { false, true, false },
        { false, true, true },
        { true, false, false },
        { true, false, true },
        { true, true, false },
        { true, true, true },
    };

    public static TheoryData<MovieFlag, MovieFlag, bool, bool, bool> StaleLoadInterleaves
    {
        get
        {
            var data = new TheoryData<MovieFlag, MovieFlag, bool, bool, bool>();
            foreach (var actorFlag in new[] { MovieFlag.Watched, MovieFlag.NotInterested, MovieFlag.Watchlist })
            {
                foreach (var siblingFlag in new[] { MovieFlag.Watched, MovieFlag.NotInterested, MovieFlag.Watchlist })
                {
                    if (actorFlag == siblingFlag)
                    {
                        continue;
                    }

                    foreach (var actorSavesLast in new[] { true, false })
                    {
                        foreach (var actorMarks in new[] { true, false })
                        {
                            foreach (var siblingMarks in new[] { true, false })
                            {
                                data.Add(actorFlag, siblingFlag, actorSavesLast, actorMarks, siblingMarks);
                            }
                        }
                    }
                }
            }

            return data;
        }
    }

    public static TheoryData<MovieFlag, bool> SameTargetFeedbackOperations => new()
    {
        { MovieFlag.Watched, true },
        { MovieFlag.Watched, false },
        { MovieFlag.NotInterested, true },
        { MovieFlag.NotInterested, false },
    };

    [Theory]
    [MemberData(nameof(FeedbackCombinations))]
    public async Task FeedbackMarks_EveryCombination_RoundTripsThroughReloadBrowseAndDetails(
        bool isWatched,
        bool isNotInterested,
        bool isInWatchlist)
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = await SeedAsync(MovieCatalogSeed.Create("Combination", "feedback-combination"));
        var id = movie.Id.ShouldNotBeNull().Value;

        // act
        if (isInWatchlist)
        {
            await ExpectNoContentAsync(client, WatchlistRoute(id), HttpMethod.Post, cancellationToken);
        }

        if (isWatched)
        {
            await ExpectNoContentAsync(client, WatchedRoute(id), HttpMethod.Post, cancellationToken);
        }

        if (isNotInterested)
        {
            await ExpectNoContentAsync(client, NotInterestedRoute(id), HttpMethod.Post, cancellationToken);
        }

        var persisted = await ReadPersistedFlagsAsync(id, cancellationToken);
        var browse = await ReadAsync(
            client,
            $"{BrowseRoute}?sort=Title&direction=Ascending&page=1&pageSize=20",
            cancellationToken);
        var details = await ReadAsync(client, DetailsRoute(id), cancellationToken);

        // assert
        persisted.ShouldBe((isWatched, isNotInterested, isInWatchlist));
        var browseItem = browse.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("id").GetInt32() == id);
        AssertFlags(browseItem, isWatched, isNotInterested, isInWatchlist);
        AssertFlags(details, isWatched, isNotInterested, isInWatchlist);
    }

    [Theory]
    [MemberData(nameof(StaleLoadInterleaves))]
    public async Task FeedbackMarks_StaleLoadSiblingInterleave_PreservesBothChanges(
        MovieFlag actorFlag,
        MovieFlag siblingFlag,
        bool actorSavesLast,
        bool actorMarks,
        bool siblingMarks)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await Fixture.Postgres.ResetAsync(cancellationToken);
        var initial = InitialFlags(actorFlag, actorMarks, siblingFlag, siblingMarks);
        var movie = await SeedFlagsAsync(initial, "stale-load-interleave");
        var id = movie.Id.ShouldNotBeNull().Value;
        await using var actorContext = Fixture.Postgres.CreateMigratedContext();
        await using var siblingContext = Fixture.Postgres.CreateMigratedContext();
        var actorRepository = Repository(actorContext);
        var siblingRepository = Repository(siblingContext);
        var actor = (await actorRepository.GetAsync(new MovieId(id), cancellationToken)).ShouldNotBeNull();
        var sibling = (await siblingRepository.GetAsync(new MovieId(id), cancellationToken)).ShouldNotBeNull();

        // act
        Change(actor, actorFlag, actorMarks);
        Change(sibling, siblingFlag, siblingMarks);
        if (actorSavesLast)
        {
            await siblingRepository.SaveChangesAsync(cancellationToken);
            await actorRepository.SaveChangesAsync(cancellationToken);
        }
        else
        {
            await actorRepository.SaveChangesAsync(cancellationToken);
            await siblingRepository.SaveChangesAsync(cancellationToken);
        }

        // assert
        var expected = initial;
        expected[(int)actorFlag] = actorMarks;
        expected[(int)siblingFlag] = siblingMarks;
        (await ReadPersistedFlagsAsync(id, cancellationToken))
            .ShouldBe((expected[0], expected[1], expected[2]));
    }

    [Theory]
    [MemberData(nameof(SameTargetFeedbackOperations))]
    public async Task FeedbackMarks_SameTargetOverlapThenObservedRepeat_ConflictsWithoutSaving(
        MovieFlag flag,
        bool mark)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await Fixture.Postgres.ResetAsync(cancellationToken);
        var initial = new bool[3];
        initial[(int)flag] = !mark;
        var movie = await SeedFlagsAsync(initial, "same-target-overlap");
        var id = movie.Id.ShouldNotBeNull().Value;
        await using var firstContext = Fixture.Postgres.CreateMigratedContext();
        await using var secondContext = Fixture.Postgres.CreateMigratedContext();
        var firstRepository = Repository(firstContext);
        var secondRepository = Repository(secondContext);
        var first = (await firstRepository.GetAsync(new MovieId(id), cancellationToken)).ShouldNotBeNull();
        var second = (await secondRepository.GetAsync(new MovieId(id), cancellationToken)).ShouldNotBeNull();

        // act
        Change(first, flag, mark);
        Change(second, flag, mark);
        await firstRepository.SaveChangesAsync(cancellationToken);
        await secondRepository.SaveChangesAsync(cancellationToken);
        await using var observerContext = Fixture.Postgres.CreateMigratedContext();
        var observer = (await Repository(observerContext).GetAsync(new MovieId(id), cancellationToken))
            .ShouldNotBeNull();
        var conflict = Should.Throw<InvalidTransitionException>(() => Change(observer, flag, mark));

        // assert
        var (action, state) = ExpectedConflict(flag, mark);
        conflict.Action.ShouldBe(action);
        conflict.State.ShouldBe(state);
        var persisted = await ReadPersistedFlagsAsync(id, cancellationToken);
        FlagValue(persisted, flag).ShouldBe(mark);
    }

    private static void AssertFlags(JsonElement element, bool isWatched, bool isNotInterested, bool isInWatchlist)
    {
        element.GetProperty("isWatched").GetBoolean().ShouldBe(isWatched);
        element.GetProperty("isNotInterested").GetBoolean().ShouldBe(isNotInterested);
        element.GetProperty("isInWatchlist").GetBoolean().ShouldBe(isInWatchlist);
    }

    private static async Task ExpectNoContentAsync(
        HttpClient client,
        string route,
        HttpMethod method,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, route);
        using var response = await client.SendAsync(request, cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static async Task<JsonElement> ReadAsync(
        HttpClient client,
        string route,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(route, cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    private static bool[] InitialFlags(
        MovieFlag first,
        bool firstMarks,
        MovieFlag second,
        bool secondMarks)
    {
        var flags = new bool[3];
        flags[(int)first] = !firstMarks;
        flags[(int)second] = !secondMarks;
        return flags;
    }

    private static bool FlagValue(
        (bool IsWatched, bool IsNotInterested, bool IsInWatchlist) flags,
        MovieFlag flag) =>
        flag switch
        {
            MovieFlag.Watched => flags.IsWatched,
            MovieFlag.NotInterested => flags.IsNotInterested,
            MovieFlag.Watchlist => flags.IsInWatchlist,
            _ => throw new ArgumentOutOfRangeException(nameof(flag), flag, null),
        };

    private static void Change(Movie movie, MovieFlag flag, bool mark)
    {
        switch (flag)
        {
            case MovieFlag.Watched:
                if (mark)
                {
                    movie.MarkWatched();
                }
                else
                {
                    movie.UnmarkWatched();
                }

                break;
            case MovieFlag.NotInterested:
                if (mark)
                {
                    movie.MarkNotInterested();
                }
                else
                {
                    movie.UnmarkNotInterested();
                }

                break;
            case MovieFlag.Watchlist:
                if (mark)
                {
                    movie.AddToWatchlist();
                }
                else
                {
                    movie.RemoveFromWatchlist();
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
        }
    }

    private static (string Action, string State) ExpectedConflict(MovieFlag flag, bool mark) =>
        (flag, mark) switch
        {
            (MovieFlag.Watched, true) => (nameof(Movie.MarkWatched), "Watched"),
            (MovieFlag.Watched, false) => (nameof(Movie.UnmarkWatched), "NotWatched"),
            (MovieFlag.NotInterested, true) => (nameof(Movie.MarkNotInterested), "NotInterested"),
            (MovieFlag.NotInterested, false) => (nameof(Movie.UnmarkNotInterested), "Interested"),
            _ => throw new ArgumentOutOfRangeException(nameof(flag), flag, null),
        };

    private EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(db, Clock, Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));

    private async Task<MovieRecord> SeedFlagsAsync(bool[] flags, string pathSuffix)
    {
        var movie = MovieCatalogSeed.Create("Feedback Flags", pathSuffix);
        movie.IsWatched = flags[(int)MovieFlag.Watched];
        movie.IsNotInterested = flags[(int)MovieFlag.NotInterested];
        movie.IsInWatchlist = flags[(int)MovieFlag.Watchlist];
        return await SeedAsync(movie);
    }

    private async Task<(bool IsWatched, bool IsNotInterested, bool IsInWatchlist)> ReadPersistedFlagsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var movieId = new MovieId(id);
        await using var context = Fixture.Postgres.CreateMigratedContext();
        var persisted = await context.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == movieId)
            .Select(movie => new
            {
                movie.IsWatched,
                movie.IsNotInterested,
                movie.IsInWatchlist,
            })
            .SingleAsync(cancellationToken);
        return (persisted.IsWatched, persisted.IsNotInterested, persisted.IsInWatchlist);
    }

    private static string DetailsRoute(int id) => $"/api/movies/{id}";

    private static string WatchlistRoute(int id) => $"/api/movies/{id}/watchlist";

    private static string WatchedRoute(int id) => $"/api/movies/{id}/watched";

    private static string NotInterestedRoute(int id) => $"/api/movies/{id}/not-interested";

    private static List<(int Id, string Name)> Facets(JsonElement body) =>
        [.. body.EnumerateArray().Select(Facet)];

    private static List<(int Id, string Name, int MovieCount)> GenreFacets(JsonElement body) =>
        [.. body.EnumerateArray().Select(GenreFacet)];

    private static (int Id, string Name) Facet(JsonElement element)
    {
        var name = element.GetProperty("name").GetString().ShouldNotBeNull();
        return (element.GetProperty("id").GetInt32(), name);
    }

    private static (int Id, string Name, int MovieCount) GenreFacet(JsonElement element)
    {
        var name = element.GetProperty("name").GetString().ShouldNotBeNull();
        return (element.GetProperty("id").GetInt32(), name, element.GetProperty("movieCount").GetInt32());
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

    private static void AssertGenreShape(JsonElement body)
    {
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        foreach (var item in body.EnumerateArray())
        {
            ShouldCarryExactly(item, "id", "name", "movieCount");
            item.GetProperty("id").ValueKind.ShouldBe(JsonValueKind.Number);
            item.GetProperty("name").ValueKind.ShouldBe(JsonValueKind.String);
            item.GetProperty("movieCount").ValueKind.ShouldBe(JsonValueKind.Number);
        }
    }

    private static void ShouldCarryExactly(JsonElement element, params string[] names) =>
        element.EnumerateObject().Select(property => property.Name).ShouldBe(names, ignoreOrder: true);

    private async Task<List<(int Id, string Name, int MovieCount)>> ReadPersistedGenresAsync()
    {
        await using var context = Fixture.Postgres.CreateMigratedContext();
        var genres = await context.Genres.AsNoTracking()
            .Include(genre => genre.Movies)
            .OrderBy(genre => genre.Name)
            .ThenBy(genre => genre.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        return [.. genres.Select(genre => (genre.Id, genre.Name, genre.Movies.Count))];
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

    public enum MovieFlag
    {
        Watched = 0,
        NotInterested = 1,
        Watchlist = 2,
    }
}
