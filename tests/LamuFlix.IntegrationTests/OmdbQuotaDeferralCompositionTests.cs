using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class OmdbQuotaDeferralCompositionTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string FoundBody = """
        {
          "Response": "True",
          "Title": "Solaris",
          "Year": "1972",
          "Runtime": "167 min",
          "Plot": "A psychologist...",
          "imdbRating": "8.0",
          "imdbID": "tt0069293"
        }
        """;

    private static readonly DateTimeOffset BeforeMidnight = new(2026, 9, 30, 23, 59, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NextUtcMidnight = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Deny_ZeroTransportPendingDeferralSameAttemptNoImportGateCall()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var time = new AdjustableTimeProvider(BeforeMidnight);
        var gate = new RecordingGate(admit: false);
        var transport = new RecordingTransport(FoundBody);
        var (handler, repository) = Compose(db, time, gate, transport);
        var movie = await SeedAsync(repository, ct);

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.QuotaDeferred(NextUtcMidnight));
        gate.Calls.ShouldBe(1);
        transport.Calls.ShouldBe(0);
        var pending = await ReadFreshAsync(movie.Id, ct);
        pending.Status.ShouldBe(EnrichmentStatus.Pending);
        pending.EnrichmentDeferredUntil.ShouldBe(NextUtcMidnight);
        pending.EnrichmentFailureCategory.ShouldBeNull();
        pending.EnrichmentAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task ReopenAtUtcMidnight_ClaimsRedispatchesAndLooksUpWithoutUserRequest()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var time = new AdjustableTimeProvider(BeforeMidnight);
        var gate = new RecordingGate(admit: false);
        var transport = new RecordingTransport(FoundBody);
        var (handler, repository) = Compose(db, time, gate, transport);
        var movie = await SeedAsync(repository, ct);
        (await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct))
            .ShouldBeOfType<ProcessEnrichmentOutcome.QuotaDeferred>();

        gate.Admit = true;
        time.Set(NextUtcMidnight.AddMinutes(10));

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Completed(Claimed: true));
        gate.Calls.ShouldBe(2);
        transport.Calls.ShouldBe(1);
        var enriched = await ReadFreshAsync(movie.Id, ct);
        enriched.Status.ShouldBe(EnrichmentStatus.Enriched);
        enriched.EnrichmentDeferredUntil.ShouldBeNull();
        enriched.ImdbId.ShouldNotBeNull();
    }

    private static (ProcessEnrichmentCommandHandler Handler, EfMovieRepository Repository) Compose(
        LamuFlixDbContext db,
        TimeProvider time,
        RecordingGate gate,
        HttpMessageHandler transport)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOmdbRequestGate>(gate);
        var scopeFactory = services.BuildServiceProvider();
        var handler = new MetadataQuotaHandler(scopeFactory.GetRequiredService<IServiceScopeFactory>())
        {
            InnerHandler = transport,
        };
        var httpClient = new HttpClient(handler);
        var provider = new OmdbMetadataProvider(
            httpClient,
            Options.Create(new OmdbOptions
            {
                ApiKey = "test-key",
                BaseUrl = "http://localhost:1",
            }),
            time,
            NullLogger<OmdbMetadataProvider>.Instance,
            new MetadataProviderHealthState());
        var repository = new EfMovieRepository(
            db,
            time,
            Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);
        var commandHandler = new ProcessEnrichmentCommandHandler(
            repository,
            provider,
            new NoOpEnrichmentQueue(),
            Options.Create(new EnrichmentOptions { MaxAttempts = 3 }),
            time,
            NullLogger<ProcessEnrichmentCommandHandler>.Instance);
        return (commandHandler, repository);
    }

    private async Task<LamuFlix.Infrastructure.Persistence.Records.MovieRecord> ReadFreshAsync(
        MovieId id,
        CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        return await db.Movies.AsNoTracking().SingleAsync(movie => movie.Id == id, ct);
    }

    private static async Task<Movie> SeedAsync(EfMovieRepository repository, CancellationToken ct)
    {
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Solaris",
            new LibraryPath("C:/library/solaris"),
            new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        return movie;
    }

    private sealed class RecordingGate(bool admit) : IOmdbRequestGate
    {
        public bool Admit { get; set; } = admit;

        public int Calls { get; private set; }

        public Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Admit);
        }
    }

    private sealed class RecordingTransport(string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
                RequestMessage = request,
            });
        }
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        public override DateTimeOffset GetUtcNow() => current;

        public void Set(DateTimeOffset value) => current = value;
    }

    private sealed class NoOpEnrichmentQueue : IEnrichmentQueue
    {
        public Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct) => Task.CompletedTask;

        public Task EnqueueAsync(FeaturesRequested message, CancellationToken ct) => Task.CompletedTask;
    }
}
