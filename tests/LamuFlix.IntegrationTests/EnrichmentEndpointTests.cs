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
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class EnrichmentEndpointTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const int KnownId = 7;
    private const int UnknownId = 999;
    private const string ProblemDetailsJson = "application/problem+json";

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IEnrichmentQueue queue = Substitute.For<IEnrichmentQueue>();
    private readonly IEnrichmentOutbox outbox = Substitute.For<IEnrichmentOutbox>();
    private readonly Movie movie = Movie.Rehydrate(
        new MovieId(KnownId),
        "Heat",
        new LibraryPath("C:/library/Heat"),
        new MediaFormat(".mkv"),
        false,
        null,
        EnrichmentStatus.NotFound,
        null,
        0,
        null,
        null);

    [Fact]
    public async Task RequestEnrichment_MissingMetadataMovie_ReturnsAcceptedWithLocationAndEmptyBody()
    {
        // arrange
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(movie);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync($"/api/movies/{KnownId}/enrichment", null, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldNotBeNull().ToString().ShouldBe($"/api/movies/{KnownId}");
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(string.Empty);
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await queue.DidNotReceive()
            .EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RequestEnrichment_NonpositiveId_ReturnsUnprocessableEntityProblem(int id)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync($"/api/movies/{id}/enrichment", null, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name).ShouldContain("id");
        await movies.DidNotReceive().GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestEnrichment_PendingMovie_ReturnsConflictProblemAndDoesNotEnqueue()
    {
        // arrange
        var pending = Movie.Rehydrate(
            new MovieId(KnownId),
            "Heat",
            new LibraryPath("C:/library/Heat"),
            new MediaFormat(".mkv"),
            false,
            null,
            EnrichmentStatus.Pending,
            null,
            0,
            null,
            null);
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(pending);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync($"/api/movies/{KnownId}/enrichment", null, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status409Conflict);
        problem.GetProperty("title").GetString().ShouldBe("Conflict");
        problem.GetRawText().ShouldNotContain(nameof(InvalidTransitionException));
        pending.Status.ShouldBe(EnrichmentStatus.Pending);
        await queue.DidNotReceive()
            .EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
        ConflictMetadata(host, "RequestEnrichment").ShouldContain(item =>
            item.StatusCode == StatusCodes.Status409Conflict && item.Type == typeof(ProblemDetails));
    }

    [Fact]
    public async Task RequestEnrichment_UnknownId_ReturnsNotFoundProblem()
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync($"/api/movies/{UnknownId}/enrichment", null, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain(nameof(NotFoundException));
        await queue.DidNotReceive()
            .EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestEnrichment_NonIntegerId_ReturnsFrameworkBadRequestProblem()
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync("/api/movies/abc/enrichment", null, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(400);
        problem.GetProperty("title").GetString().ShouldBe("Bad Request");
        problem.TryGetProperty("detail", out _).ShouldBeFalse();
        problem.TryGetProperty("errors", out _).ShouldBeFalse();
        problem.GetRawText().ShouldNotContain("BadHttpRequestException");
    }

    [Fact]
    public async Task MatchImdbId_ValidId_ReturnsAcceptedWithLocationAndRequeuesWithManualId()
    {
        // arrange
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(movie);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/movies/{KnownId}/imdb",
            new { imdbId = "tt0083658" },
            cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldNotBeNull().ToString().ShouldBe($"/api/movies/{KnownId}");
        movie.ManualImdbId.ShouldBe(new ImdbId("tt0083658"));
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        outbox.Received(1).Stage(
            Arg.Is<EnrichmentRequested>(message => message.MovieId == new MovieId(KnownId) && message.Attempt == 1));
        await queue.DidNotReceive()
            .EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("tt123")]
    [InlineData("https://www.imdb.com/title/tt0083658/")]
    public async Task MatchImdbId_MalformedImdbId_ReturnsBadRequestValidationProblemAndDoesNotEnqueue(string imdbId)
    {
        // arrange
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(movie);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/movies/{KnownId}/imdb",
            new { imdbId },
            cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name).ShouldContain("imdbId");
        movie.ManualImdbId.ShouldBeNull();
        await queue.DidNotReceive()
            .EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MatchImdbId_UnknownId_ReturnsNotFoundProblemAndDoesNotEnqueue()
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/movies/{UnknownId}/imdb",
            new { imdbId = "tt0083658" },
            cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain(nameof(NotFoundException));
        await queue.DidNotReceive()
            .EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MatchImdbId_IdHeldByAnotherMovie_ReturnsConflictProblemNamingTheHolderAndDoesNotStage()
    {
        // arrange
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(movie);
        movies.FindMovieIdByImdbIdAsync(new ImdbId("tt0083658"), Arg.Any<CancellationToken>())
            .Returns(new MovieId(99));
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/movies/{KnownId}/imdb",
            new { imdbId = "tt0083658" },
            cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status409Conflict);
        problem.GetProperty("detail").GetString().ShouldBe("IMDb id tt0083658 is already assigned to movie 99.");
        problem.GetProperty("heldByMovieId").GetInt32().ShouldBe(99);
        problem.GetRawText().ShouldNotContain(nameof(ImdbIdConflictException));
        movie.ManualImdbId.ShouldBeNull();
        movie.Status.ShouldBe(EnrichmentStatus.NotFound);
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        outbox.DidNotReceive().Stage(Arg.Any<EnrichmentRequested>());
        await queue.DidNotReceive()
            .EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
        ConflictMetadata(host, "MatchMovieImdbId").ShouldContain(item =>
            item.StatusCode == StatusCodes.Status409Conflict && item.Type == typeof(ProblemDetails));
    }

    [Fact]
    public async Task MatchImdbId_LoserOfAConcurrentMatch_ReturnsConflictProblemNamingTheWinnerAndDoesNotPublish()
    {
        // arrange
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(movie);
        movies.FindMovieIdByImdbIdAsync(new ImdbId("tt0083658"), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<MovieId?>(null), Task.FromResult<MovieId?>(new MovieId(99)));
        movies.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateImdbIdException("IX_movies_imdb_id"));
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/movies/{KnownId}/imdb",
            new { imdbId = "tt0083658" },
            cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status409Conflict);
        problem.GetProperty("detail").GetString().ShouldBe("IMDb id tt0083658 is already assigned to movie 99.");
        problem.GetProperty("heldByMovieId").GetInt32().ShouldBe(99);
        problem.GetRawText().ShouldNotContain(nameof(DuplicateImdbIdException));
        await queue.DidNotReceive()
            .EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
    }

    private WebApplicationFactory<Program> CreateHost() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(movies);
            services.AddSingleton(queue);
            services.AddSingleton(outbox);
        }));

    private static IEnumerable<IProducesResponseTypeMetadata> ConflictMetadata(
        WebApplicationFactory<Program> host,
        string endpointName) =>
        host.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == endpointName)
            .Metadata
            .OfType<IProducesResponseTypeMetadata>();
}

[Collection(nameof(PostgresCollection))]
public sealed class EnrichmentEndpointConcurrencyTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string ImdbIdUnderContention = "tt0083658";
    private const string ProblemDetailsJson = "application/problem+json";
    private static readonly TimeSpan OverlapDeadline = TimeSpan.FromSeconds(30);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task MatchImdbId_TwoMoviesClaimingTheSameIdConcurrently_OneAcceptedOneConflictAndOnlyTheWinnerStages()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var firstId = await SeedNotFoundMovieAsync("Concurrent First", "concurrent-first", cancellationToken);
        var secondId = await SeedNotFoundMovieAsync("Concurrent Second", "concurrent-second", cancellationToken);
        var gate = new SaveOverlapGate(2);
        await using var host = CreateRealHost(gate);
        using var client = host.CreateClient();

        // act
        var first = MatchAsync(client, firstId, cancellationToken);
        var second = MatchAsync(client, secondId, cancellationToken);
        var responses = await Task.WhenAll(first, second);

        // assert
        gate.Arrivals.ShouldBe(2);
        var acceptedResponses = responses.Where(response => response.StatusCode == HttpStatusCode.Accepted).ToArray();
        acceptedResponses.Length.ShouldBe(1);
        var conflictResponses = responses.Where(response => response.StatusCode == HttpStatusCode.Conflict).ToArray();
        conflictResponses.Length.ShouldBe(1);
        var conflict = conflictResponses[0];
        var accepted = acceptedResponses[0];
        var winnerLocation = accepted.Headers.Location.ShouldNotBeNull().ToString();
        var winnerId = winnerLocation == $"/api/movies/{firstId.Value}" ? firstId : secondId;
        var loserId = winnerId == firstId ? secondId : firstId;
        conflict.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        var problem = await conflict.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        problem.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status409Conflict);
        problem.GetProperty("heldByMovieId").GetInt32().ShouldBe(winnerId.Value);

        await using var verification = fixture.CreateMigratedContext();
        var holders = await verification.Movies.AsNoTracking()
            .Where(movie => movie.ManualImdbId == new ImdbId(ImdbIdUnderContention))
            .Select(movie => movie.Id)
            .ToArrayAsync(cancellationToken);
        holders.ShouldBe([winnerId]);
        var loser = await verification.Movies.AsNoTracking()
            .SingleAsync(movie => movie.Id == loserId, cancellationToken);
        loser.ManualImdbId.ShouldBeNull();
        loser.Status.ShouldBe(EnrichmentStatus.NotFound);
        var staged = await ReadStagedMovieIdsAsync(verification, cancellationToken);
        staged.ShouldBe([winnerId.Value]);
    }

    [Fact]
    public async Task MatchImdbId_LaterRequestAgainstTheManualHolder_ReturnsConflictNamingTheHolderAndStagesNothing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var winnerId = await SeedNotFoundMovieAsync("Sequential Winner", "sequential-winner", cancellationToken);
        var laterId = await SeedNotFoundMovieAsync("Sequential Later", "sequential-later", cancellationToken);
        var gate = new SaveOverlapGate(1);
        await using var host = CreateRealHost(gate);
        using var client = host.CreateClient();

        // act
        var accepted = await MatchAsync(client, winnerId, cancellationToken);
        var later = await MatchAsync(client, laterId, cancellationToken);

        // assert
        accepted.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        later.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await later.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        problem.GetProperty("heldByMovieId").GetInt32().ShouldBe(winnerId.Value);
        await using var verification = fixture.CreateMigratedContext();
        var losers = await verification.Movies.AsNoTracking()
            .Where(movie => movie.Id == laterId)
            .Select(movie => movie.ManualImdbId)
            .ToArrayAsync(cancellationToken);
        losers.ShouldBe([null]);
        (await ReadStagedMovieIdsAsync(verification, cancellationToken)).ShouldBe([winnerId.Value]);
    }

    [Fact]
    public async Task MatchImdbId_IdHeldByMetadata_ReturnsConflictNamingTheMetadataHolderAndStagesNothing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var metadataHolderId = await SeedMetadataHolderAsync(cancellationToken);
        var claimantId = await SeedNotFoundMovieAsync("Metadata Claimant", "metadata-claimant", cancellationToken);
        var gate = new SaveOverlapGate(1);
        await using var host = CreateRealHost(gate);
        using var client = host.CreateClient();

        // act
        var response = await MatchAsync(client, claimantId, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        problem.GetProperty("heldByMovieId").GetInt32().ShouldBe(metadataHolderId.Value);
        await using var verification = fixture.CreateMigratedContext();
        var claimant = await verification.Movies.AsNoTracking()
            .SingleAsync(movie => movie.Id == claimantId, cancellationToken);
        claimant.ManualImdbId.ShouldBeNull();
        (await ReadStagedMovieIdsAsync(verification, cancellationToken)).ShouldBeEmpty();
    }

    private static Task<HttpResponseMessage> MatchAsync(
        HttpClient client,
        MovieId id,
        CancellationToken cancellationToken) =>
        Task.Run(
            () => client.PutAsJsonAsync(
                $"/api/movies/{id.Value}/imdb",
                new { imdbId = ImdbIdUnderContention },
                cancellationToken),
            cancellationToken);

    private static async Task<int[]> ReadStagedMovieIdsAsync(
        LamuFlixDbContext context,
        CancellationToken cancellationToken)
    {
        var payloads = await context.OutboxMessages.AsNoTracking()
            .Select(message => message.Payload)
            .ToListAsync(cancellationToken);
        var ids = new List<int>(payloads.Count);
        foreach (var payload in payloads)
        {
            var request = JsonSerializer.Deserialize<EnrichmentRequested>(
                payload,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (request is not null)
            {
                ids.Add(request.MovieId.Value);
            }
        }

        ids.Sort();
        return [.. ids];
    }

    private WebApplicationFactory<Program> CreateRealHost(SaveOverlapGate gate) =>
        new ApiHostFactory(
                retainSweeper: false,
                additionalSettings: [new("ConnectionStrings:DefaultConnection", fixture.ConnectionString)],
                disableEnrichmentConsumer: true)
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<LamuFlixDbContext>>();
                services.AddDbContext<LamuFlixDbContext>(options => options
                    .UseNpgsql(fixture.ConnectionString, npgsql => npgsql.UseVector())
                    .AddInterceptors(gate));
            }));

    private async Task<MovieId> SeedNotFoundMovieAsync(
        string title,
        string pathSuffix,
        CancellationToken cancellationToken)
    {
        await using var context = fixture.CreateMigratedContext();
        var record = MovieCatalogSeed.Create(title, pathSuffix);
        record.Status = EnrichmentStatus.NotFound;
        record.AddedAt = MovieCatalogSeed.FixedTime;
        return RequiredId(await MovieCatalogSeed.AddAsync(context, record, cancellationToken));
    }

    private async Task<MovieId> SeedMetadataHolderAsync(CancellationToken cancellationToken)
    {
        await using var context = fixture.CreateMigratedContext();
        var record = MovieCatalogSeed.Create("Metadata Holder", "metadata-holder");
        record.Status = EnrichmentStatus.Enriched;
        record.AddedAt = MovieCatalogSeed.FixedTime;
        record.MetadataTitle = "Metadata Holder";
        record.ImdbId = new ImdbId(ImdbIdUnderContention);
        return RequiredId(await MovieCatalogSeed.AddAsync(context, record, cancellationToken));
    }

    private static MovieId RequiredId(MovieRecord record) =>
        record.Id ?? throw new InvalidOperationException("The seeded movie was not assigned an id.");

    private sealed class SaveOverlapGate(int expected) : SaveChangesInterceptor
    {
        private readonly Lock sync = new();
        private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int arrivals;

        public int Arrivals => Volatile.Read(ref arrivals);

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ArgumentNullException.ThrowIfNull(eventData);
            if (ClaimsTheContendedId(eventData.Context))
            {
                throw new InvalidOperationException("The manual IMDb claim must be saved asynchronously.");
            }

            return result;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(eventData);
            if (!ClaimsTheContendedId(eventData.Context))
            {
                return result;
            }

            await ArriveAsync(cancellationToken);
            return result;
        }

        private static bool ClaimsTheContendedId(DbContext? context) =>
            context is not null
            && context.ChangeTracker.Entries<MovieRecord>().Any(entry =>
                entry.State == EntityState.Modified
                && entry.Entity.ManualImdbId == new ImdbId(ImdbIdUnderContention));

        private async Task ArriveAsync(CancellationToken cancellationToken)
        {
            lock (sync)
            {
                arrivals++;
                if (arrivals >= expected)
                {
                    released.TrySetResult();
                }
            }

            await released.Task.WaitAsync(OverlapDeadline, cancellationToken);
        }
    }
}
