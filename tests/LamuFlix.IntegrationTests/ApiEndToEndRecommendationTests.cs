using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence.Records;
using Pgvector;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(ApiEndToEndCollection))]
public sealed class ApiEndToEndRecommendationTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture)
{
    private const string BrowseRoute = "/api/movies";
    private const string RecommendationsSuffix = "/recommendations";
    private const string ForYouRoute = "/api/recommendations/for-you";
    private const string ApplicationJson = "application/json";
    private const string ProblemDetailsJson = "application/problem+json";
    private const int UnknownId = 999999;
    private const int SelectedYear = 2000;
    private const int OtherYear = 2010;
    private const int SelectedRuntime = 100;
    private const decimal SelectedRating = 8.0m;
    private const double IdenticalProfileScore = 1.0;
    private const double PartialProfileScore = 0.48112563433075205;

    private static readonly string[] DtoFieldNames =
    [
        "id",
        "title",
        "posterUrl",
        "releaseYear",
        "runtime",
        "imdbRating",
        "enrichmentStatus",
        "isInWatchlist",
        "isWatched",
        "isNotInterested",
        "score",
        "reasons",
    ];

    [Fact]
    public async Task GetRecommendations_SeededEligibleMovie_ReturnsRankedJsonWithReasons()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var data = new Fixture();
        var genre = Faker.Lorem.GetFirstWord();
        var director = Faker.Name.FullName();
        var otherDirector = Faker.Name.FullName();
        while (string.Equals(otherDirector, director, StringComparison.Ordinal))
        {
            otherDirector = Faker.Name.FullName();
        }

        var selected = EnrichedMovie(
            data.Create<string>(), "recommendations-selected", genre, director, SelectedYear, SelectedRuntime, SelectedRating);
        var identical = EnrichedMovie(
            data.Create<string>(), "recommendations-identical", genre, director, SelectedYear, SelectedRuntime, SelectedRating);
        var partial = EnrichedMovie(
            data.Create<string>(), "recommendations-partial", genre, otherDirector, OtherYear, runtime: null, rating: null);
        var selectedRecord = await SeedAsync(selected);
        var identicalRecord = await SeedAsync(identical);
        var partialRecord = await SeedAsync(partial);
        var selectedId = selectedRecord.Id.ShouldNotBeNull().Value;
        var identicalId = identicalRecord.Id.ShouldNotBeNull().Value;
        var partialId = partialRecord.Id.ShouldNotBeNull().Value;
        await SeedVectorAsync(selectedId, primary: 1f, secondary: 0f);
        await SeedVectorAsync(identicalId, primary: 0f, secondary: 1f);
        await SeedVectorAsync(partialId, primary: 0f, secondary: 1f);

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}/{selectedId}{RecommendationsSuffix}",
            cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        var items = body.EnumerateArray().ToList();
        items.Select(item => item.GetProperty("id").GetInt32()).ShouldBe([identicalId, partialId]);

        var identicalItem = items[0];
        ShouldCarryExactly(identicalItem, DtoFieldNames);
        identicalItem.GetProperty("id").GetInt32().ShouldBe(identicalId);
        identicalItem.GetProperty("title").GetString().ShouldBe(identical.Title);
        identicalItem.GetProperty("posterUrl").ValueKind.ShouldBe(JsonValueKind.Null);
        identicalItem.GetProperty("releaseYear").GetInt32().ShouldBe(SelectedYear);
        identicalItem.GetProperty("runtime").GetInt32().ShouldBe(SelectedRuntime);
        identicalItem.GetProperty("imdbRating").GetDecimal().ShouldBe(SelectedRating);
        identicalItem.GetProperty("enrichmentStatus").GetString().ShouldBe(nameof(EnrichmentStatus.Enriched));
        identicalItem.GetProperty("isInWatchlist").GetBoolean().ShouldBeFalse();
        identicalItem.GetProperty("isWatched").GetBoolean().ShouldBeFalse();
        identicalItem.GetProperty("isNotInterested").GetBoolean().ShouldBeFalse();
        identicalItem.GetProperty("score").GetDouble().ShouldBe(IdenticalProfileScore);
        ReasonTexts(identicalItem).ShouldBe(
        [
            $"Same director: {director}",
            $"Shares {genre}",
            "Similar release years",
            "Similar runtime",
            "Similar rating",
        ]);
        ReasonKinds(identicalItem).ShouldBe(
        [
            "structural",
            "structural",
            "structural",
            "structural",
            "structural",
        ]);

        var partialItem = items[1];
        ShouldCarryExactly(partialItem, DtoFieldNames);
        partialItem.GetProperty("id").GetInt32().ShouldBe(partialId);
        partialItem.GetProperty("title").GetString().ShouldBe(partial.Title);
        partialItem.GetProperty("posterUrl").ValueKind.ShouldBe(JsonValueKind.Null);
        partialItem.GetProperty("releaseYear").GetInt32().ShouldBe(OtherYear);
        partialItem.GetProperty("runtime").ValueKind.ShouldBe(JsonValueKind.Null);
        partialItem.GetProperty("imdbRating").ValueKind.ShouldBe(JsonValueKind.Null);
        partialItem.GetProperty("enrichmentStatus").GetString().ShouldBe(nameof(EnrichmentStatus.Enriched));
        partialItem.GetProperty("score").GetDouble().ShouldBe(PartialProfileScore, 1e-9);
        ReasonTexts(partialItem).ShouldBe(
        [
            $"Shares {genre}",
            "Similar release years",
        ]);
        ReasonKinds(partialItem).ShouldBe(["structural", "structural"]);
    }

    [Fact]
    public async Task GetRecommendations_UnknownMovieId_ReturnsNotFoundProblem()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}/{UnknownId}{RecommendationsSuffix}",
            cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        body.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRecommendations_PendingMovie_ReturnsOkWithAnEmptyArray()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var data = new Fixture();
        var pending = MovieCatalogSeed.Create(data.Create<string>(), "recommendations-pending");
        var pendingRecord = await SeedAsync(pending);
        var pendingId = pendingRecord.Id.ShouldNotBeNull().Value;

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}/{pendingId}{RecommendationsSuffix}",
            cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetForYou_PositiveHistory_ReturnsPersonalReasonsAndRetainsVectorlessCandidates()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var data = new Fixture();
        var genre = Faker.Lorem.GetFirstWord();
        var watched = EnrichedMovie(
            data.Create<string>(),
            "for-you-watched",
            genre,
            Faker.Name.FullName(),
            SelectedYear,
            SelectedRuntime,
            SelectedRating);
        watched.IsWatched = true;
        var enrichedCandidate = EnrichedMovie(
            data.Create<string>(), "for-you-enriched", genre, Faker.Name.FullName(), OtherYear, null, null);
        var vectorlessCandidate = EnrichedMovie(
            data.Create<string>(), "for-you-vectorless", genre, Faker.Name.FullName(), OtherYear, null, null);
        var excluded = EnrichedMovie(
            data.Create<string>(), "for-you-excluded", genre, Faker.Name.FullName(), OtherYear, null, null);
        excluded.IsNotInterested = true;

        var watchedId = (await SeedAsync(watched)).Id.ShouldNotBeNull().Value;
        var enrichedId = (await SeedAsync(enrichedCandidate)).Id.ShouldNotBeNull().Value;
        var vectorlessId = (await SeedAsync(vectorlessCandidate)).Id.ShouldNotBeNull().Value;
        var excludedId = (await SeedAsync(excluded)).Id.ShouldNotBeNull().Value;
        await SeedVectorAsync(watchedId, primary: 1f, secondary: 0f);
        await SeedVectorAsync(enrichedId, primary: 0f, secondary: 1f);

        // act
        var response = await client.GetAsync(ForYouRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var repeated = await client.GetAsync(ForYouRoute, cancellationToken);
        var repeatedBody = await repeated.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        repeated.StatusCode.ShouldBe(HttpStatusCode.OK);
        repeatedBody.GetRawText().ShouldBe(body.GetRawText());
        var items = body.EnumerateArray().ToList();
        var ids = items.Select(item => item.GetProperty("id").GetInt32()).ToList();
        ids.ShouldContain(enrichedId);
        ids.ShouldContain(vectorlessId);
        ids.ShouldNotContain(excludedId);

        var enrichedItem = items.Single(item => item.GetProperty("id").GetInt32() == enrichedId);
        ShouldCarryExactly(enrichedItem, DtoFieldNames);
        ReasonKinds(enrichedItem).ShouldContain("personal");
        ReasonTexts(enrichedItem).ShouldContain($"Matches your {genre} interest");

        var vectorlessItem = items.Single(item => item.GetProperty("id").GetInt32() == vectorlessId);
        ShouldCarryExactly(vectorlessItem, DtoFieldNames);
        ReasonTexts(vectorlessItem).ShouldContain($"Matches your {genre} interest");
    }

    [Fact]
    public async Task GetForYou_NoPositiveHistory_ReturnsOkEmptyArray()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var data = new Fixture();
        var genre = Faker.Lorem.GetFirstWord();
        var unwatched = EnrichedMovie(
            data.Create<string>(), "for-you-cold-unwatched", genre, Faker.Name.FullName(), OtherYear, null, null);
        var notInterested = EnrichedMovie(
            data.Create<string>(), "for-you-cold-not-interested", genre, Faker.Name.FullName(), OtherYear, null, null);
        notInterested.IsNotInterested = true;
        var unwatchedId = (await SeedAsync(unwatched)).Id.ShouldNotBeNull().Value;
        await SeedAsync(notInterested);
        await SeedVectorAsync(unwatchedId, primary: 1f, secondary: 0f);

        // act
        var response = await client.GetAsync(ForYouRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetForYou_FeedbackInvalidatesTheCachedProfile()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var data = new Fixture();
        var historyGenre = $"history-{Guid.NewGuid():N}";
        var candidateGenre = $"candidate-{Guid.NewGuid():N}";

        var watched = EnrichedMovie(
            data.Create<string>(),
            "for-you-cache-history",
            historyGenre,
            Faker.Name.FullName(),
            SelectedYear,
            SelectedRuntime,
            SelectedRating);
        watched.IsWatched = true;
        var candidate = EnrichedMovie(
            data.Create<string>(), "for-you-cache-candidate", candidateGenre, Faker.Name.FullName(), OtherYear, null, null);
        var watchedId = (await SeedAsync(watched)).Id.ShouldNotBeNull().Value;
        var candidateId = (await SeedAsync(candidate)).Id.ShouldNotBeNull().Value;
        await SeedVectorAsync(watchedId, primary: 1f, secondary: 0f);

        var before = await client.GetAsync(ForYouRoute, cancellationToken);
        var beforeBody = await before.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // act
        var marked = await client.PostAsync(
            $"{BrowseRoute}/{candidateId}/watched",
            content: null,
            cancellationToken);
        var after = await client.GetAsync(ForYouRoute, cancellationToken);
        var afterBody = await after.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        before.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cachedItem = beforeBody.EnumerateArray()
            .Single(item => item.GetProperty("id").GetInt32() == candidateId);
        ReasonKinds(cachedItem).ShouldNotContain("personal");
        marked.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        after.StatusCode.ShouldBe(HttpStatusCode.OK);
        var invalidatedItem = afterBody.EnumerateArray()
            .Single(item => item.GetProperty("id").GetInt32() == candidateId);
        ReasonTexts(invalidatedItem).ShouldContain($"Matches your {candidateGenre} interest");
    }

    private static MovieRecord EnrichedMovie(
        string title,
        string pathSuffix,
        string genre,
        string director,
        int year,
        int? runtime,
        decimal? rating)
    {
        var movie = MovieCatalogSeed.Create(title, pathSuffix);
        movie.Status = EnrichmentStatus.Enriched;
        movie.MetadataTitle = title;
        movie.ReleaseYear = new ReleaseYear(year, MovieCatalogSeed.FixedTime);
        movie.Genres.Add(new GenreRecord { Name = genre });
        movie.Directors.Add(new DirectorRecord { Name = director });

        if (runtime is { } minutes)
        {
            movie.RuntimeMinutes = new Runtime(minutes);
        }

        if (rating is { } value)
        {
            movie.ImdbRating = new ImdbRating(value);
        }

        return movie;
    }

    private async Task SeedVectorAsync(int movieId, float primary, float secondary)
    {
        var vector = new float[EmbeddingDimensions.Value];
        vector[0] = primary;
        vector[1] = secondary;

        await using var context = Fixture.Postgres.CreateMigratedContext();
        context.MovieFeatures.Add(new MovieFeaturesRecord
        {
            MovieId = new MovieId(movieId),
            Vector = new Vector(vector),
            ComputedAt = MovieCatalogSeed.FixedTime
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static void ShouldCarryExactly(JsonElement element, IEnumerable<string> names) =>
        element.EnumerateObject().Select(property => property.Name).ShouldBe(names, ignoreOrder: true);

    private static IEnumerable<string?> ReasonTexts(JsonElement item) =>
        item.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetProperty("text").GetString());

    private static IEnumerable<string?> ReasonKinds(JsonElement item) =>
        item.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetProperty("kind").GetString());
}
