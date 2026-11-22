using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence.Records;
using Pgvector;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class ApiEndToEndRecommendationTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture), IClassFixture<ApiEndToEndFixture>
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
    private const double IdenticalProfileScore = 1.0 / 3.0;
    private const double PartialProfileScore = 2.0 / 15.0;

    private static readonly string[] RelatedDtoFieldNames =
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
        "review",
        "score",
        "evidence",
    ];

    private static readonly string[] ForYouDtoFieldNames =
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
        "review",
        "score",
        "evidence",
        "seed",
    ];

    [Fact]
    public async Task GetRecommendations_SeededEligibleMovie_ReturnsRankedJsonWithEvidence()
    {
        // arrange
        // This suite asserts ranking order and DTO mapping, not the DEV-544 strong-match bar,
        // so the bar is neutralised to keep every eligible candidate in the rail.
        var client = await StartHostAsync(additionalSettings: NeutralStrongMatchBar());
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
        ShouldCarryExactly(identicalItem, RelatedDtoFieldNames);
        ShouldNotCarryAnyCasingOf(identicalItem, "reasons", "seed");
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
        identicalItem.GetProperty("score").GetDouble().ShouldBe(IdenticalProfileScore, 1e-9);
        EvidenceCriteria(identicalItem).ShouldBe(["Director", "Genre"]);
        EvidenceTexts(identicalItem).ShouldBe(
        [
            $"Same director: {director}",
            $"Same genres: {genre}",
        ]);
        EvidenceNames(identicalItem).ShouldBe([director, genre]);

        var partialItem = items[1];
        ShouldCarryExactly(partialItem, RelatedDtoFieldNames);
        ShouldNotCarryAnyCasingOf(partialItem, "reasons", "seed");
        partialItem.GetProperty("id").GetInt32().ShouldBe(partialId);
        partialItem.GetProperty("title").GetString().ShouldBe(partial.Title);
        partialItem.GetProperty("posterUrl").ValueKind.ShouldBe(JsonValueKind.Null);
        partialItem.GetProperty("releaseYear").GetInt32().ShouldBe(OtherYear);
        partialItem.GetProperty("runtime").ValueKind.ShouldBe(JsonValueKind.Null);
        partialItem.GetProperty("imdbRating").ValueKind.ShouldBe(JsonValueKind.Null);
        partialItem.GetProperty("enrichmentStatus").GetString().ShouldBe(nameof(EnrichmentStatus.Enriched));
        partialItem.GetProperty("score").GetDouble().ShouldBe(PartialProfileScore, 1e-9);
        EvidenceCriteria(partialItem).ShouldBe(["Genre"]);
        EvidenceTexts(partialItem).ShouldBe([$"Same genres: {genre}"]);
        EvidenceNames(partialItem).ShouldBe([genre]);
        ShouldNotCarryRetiredCopy(body);
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
    public async Task GetForYou_PositiveHistory_ReturnsSeedsAndRetainsVectorlessCandidates()
    {
        // arrange
        var client = await StartHostAsync(additionalSettings: NeutralStrongMatchBar());
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
        excluded.Review = Review.Disliked;

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
        var items = body.GetProperty("items").EnumerateArray().ToList();
        var ids = items.Select(item => item.GetProperty("id").GetInt32()).ToList();
        ids.ShouldContain(enrichedId);
        ids.ShouldContain(vectorlessId);
        ids.ShouldNotContain(excludedId);
        ids.ShouldNotContain(watchedId);

        var enrichedItem = items.Single(item => item.GetProperty("id").GetInt32() == enrichedId);
        ShouldCarryExactly(enrichedItem, ForYouDtoFieldNames);
        ShouldNotCarryAnyCasingOf(enrichedItem, "reasons");
        var enrichedSeed = enrichedItem.GetProperty("seed");
        enrichedSeed.GetProperty("movieId").GetInt32().ShouldBe(watchedId);
        enrichedSeed.GetProperty("title").GetString().ShouldBe(watched.Title);
        enrichedSeed.GetProperty("signal").GetString().ShouldBe("watched");
        EvidenceCriteria(enrichedItem).ShouldBe(["Genre"]);
        EvidenceTexts(enrichedItem).ShouldBe([$"Same genres: {genre}"]);
        EvidenceNames(enrichedItem).ShouldBe([genre]);

        var vectorlessItem = items.Single(item => item.GetProperty("id").GetInt32() == vectorlessId);
        ShouldCarryExactly(vectorlessItem, ForYouDtoFieldNames);
        ShouldNotCarryAnyCasingOf(vectorlessItem, "reasons");
        vectorlessItem.GetProperty("seed").GetProperty("movieId").GetInt32().ShouldBe(watchedId);
        vectorlessItem.GetProperty("seed").GetProperty("signal").GetString().ShouldBe("watched");
        vectorlessItem.GetProperty("evidence").ValueKind.ShouldBe(JsonValueKind.Array);
        ShouldNotCarryRetiredCopy(body);
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
        notInterested.Review = Review.Disliked;
        var unwatchedId = (await SeedAsync(unwatched)).Id.ShouldNotBeNull().Value;
        await SeedAsync(notInterested);
        await SeedVectorAsync(unwatchedId, primary: 1f, secondary: 0f);

        // act
        var response = await client.GetAsync(ForYouRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        body.ValueKind.ShouldBe(JsonValueKind.Object);
        body.GetProperty("items").EnumerateArray().ShouldBeEmpty();
        body.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task GetForYou_MarkWatched_RemovesMovieFromResults()
    {
        // arrange
        var client = await StartHostAsync(additionalSettings: NeutralStrongMatchBar());
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
        var cachedItem = beforeBody.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("id").GetInt32() == candidateId);
        cachedItem.GetProperty("seed").GetProperty("movieId").GetInt32().ShouldBe(watchedId);
        cachedItem.GetProperty("seed").GetProperty("title").GetString().ShouldBe(watched.Title);
        cachedItem.GetProperty("seed").GetProperty("signal").GetString().ShouldBe("watched");
        ShouldNotCarryAnyCasingOf(cachedItem, "reasons");
        marked.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        after.StatusCode.ShouldBe(HttpStatusCode.OK);
        afterBody.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ShouldNotContain(candidateId);
    }

    [Fact]
    public async Task GetRecommendations_SeedFranchiseMembers_AreGroupedContiguouslyAtFront()
    {
        // arrange
        var client = await StartHostAsync(additionalSettings: NeutralStrongMatchBar());
        var cancellationToken = TestContext.Current.CancellationToken;
        var data = new Fixture();
        const int franchiseId = 900;
        await SeedCollectionAsync(franchiseId, "Franchise 900");

        var genre = Faker.Lorem.GetFirstWord();
        var director = Faker.Name.FullName();
        var selected = EnrichedMovie(
            data.Create<string>(), "franchise-selected", genre, director, SelectedYear, SelectedRuntime, SelectedRating);
        selected.CollectionId = franchiseId;
        var franchiseA = EnrichedMovie(
            data.Create<string>(), "franchise-first", genre, director, SelectedYear, SelectedRuntime, SelectedRating);
        franchiseA.CollectionId = franchiseId;
        var franchiseB = EnrichedMovie(
            data.Create<string>(), "franchise-second", genre, director, SelectedYear, SelectedRuntime, SelectedRating);
        franchiseB.CollectionId = franchiseId;

        var selectedRecord = await SeedAsync(selected);
        var franchiseARecord = await SeedAsync(franchiseA);
        var franchiseBRecord = await SeedAsync(franchiseB);
        var selectedId = selectedRecord.Id.ShouldNotBeNull().Value;
        int[] franchiseIds = [franchiseARecord.Id.ShouldNotBeNull().Value, franchiseBRecord.Id.ShouldNotBeNull().Value];
        await SeedVectorAsync(selectedId, primary: 1f, secondary: 0f);

        for (var index = 0; index < 8; index++)
        {
            var other = EnrichedMovie(
                data.Create<string>(),
                $"franchise-other-{index}",
                $"{genre}-{index}",
                Faker.Name.FullName(),
                OtherYear,
                SelectedRuntime,
                SelectedRating);
            var otherRecord = await SeedAsync(other);
            await SeedVectorAsync(otherRecord.Id.ShouldNotBeNull().Value, primary: 0f, secondary: 1f);
        }

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}/{selectedId}{RecommendationsSuffix}",
            cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ids = body.EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ToList();
        ids.Count.ShouldBe(10);
        ids.ShouldNotBeEmpty();
        ids.Take(2).ShouldBe(franchiseIds.Order());
        ids.Skip(2).ShouldNotContain(id => franchiseIds.Contains(id));
    }

    private static IEnumerable<KeyValuePair<string, string?>> NeutralStrongMatchBar() =>
    [
        new(
            $"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.MinimumSimilarity)}",
            "0"),
    ];

    private async Task SeedCollectionAsync(int id, string name)
    {
        await using var context = Fixture.Postgres.CreateMigratedContext();
        context.Collections.Add(new CollectionRecord { Id = id, Name = name });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
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

    private static void ShouldNotCarryAnyCasingOf(JsonElement element, params string[] names) =>
        element.EnumerateObject()
            .ShouldNotContain(property => names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)));

    private static void ShouldNotCarryRetiredCopy(JsonElement body) =>
        body.GetRawText().ShouldNotContain("Matches your viewing history");

    private static IEnumerable<string?> EvidenceCriteria(JsonElement item) =>
        item.GetProperty("evidence").EnumerateArray().Select(evidence => evidence.GetProperty("criterion").GetString());

    private static IEnumerable<string?> EvidenceTexts(JsonElement item) =>
        item.GetProperty("evidence").EnumerateArray().Select(evidence => evidence.GetProperty("text").GetString());

    private static IEnumerable<string> EvidenceNames(JsonElement item) =>
        item.GetProperty("evidence").EnumerateArray()
            .SelectMany(evidence => evidence.GetProperty("matchedNames").EnumerateArray())
            .Select(name => name.GetString() ?? string.Empty);
}
