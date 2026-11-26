using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class EfRecommendationCandidateSourceBudgetTests(PostgresFixture fixture) : IAsyncLifetime, IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetOutsideLibraryRecommendationsAsync_NoSharedAllowance_AdmitsOnlyFreshCachedEvidence()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedCandidatesAsync(ct);
        await using (var writeDb = fixture.CreateMigratedContext())
        {
            await new EfCandidateEvidenceStore(writeDb, new FixedTimeProvider(Now)).WriteAsync(
                902,
                Complete(),
                ct);
            writeDb.DailyRequestUsages.Add(new DailyRequestUsageRecord
            {
                UsageDate = DateOnly.FromDateTime(Now.UtcDateTime),
                RequestCount = 1,
            });
            await writeDb.SaveChangesAsync(ct);
        }

        var harness = new Harness(HttpStatusCode.OK, """{ "imdb_id": "tt0111161" }""");
        await using var db = fixture.CreateMigratedContext();
        var source = harness.Source(db, dailyLimit: 1);

        // act
        var snapshot = await source.GetOutsideLibraryRecommendationsAsync(ct);

        // assert
        snapshot.AdmittedCards.ShouldNotBeNull();
        snapshot.AdmittedCards.Select(card => card.TmdbId).ShouldBe([902]);
        harness.TmdbHandler.Requests.ShouldBeEmpty();
        harness.OmdbHandler.GateAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task GetOutsideLibraryRecommendationsAsync_DisabledAllowance_CachedNextSurvivorFillsTheRow()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedCandidatesAsync(ct);
        await using (var writeDb = fixture.CreateMigratedContext())
        {
            var store = new EfCandidateEvidenceStore(writeDb, new FixedTimeProvider(Now));
            await store.WriteAsync(
                901,
                new CandidateImdbEvidence(
                    "tt9010001",
                    null,
                    5.9m,
                    2500,
                    CandidateEvidenceOutcome.Complete,
                    Now.AddDays(30)),
                ct);
            await store.WriteAsync(
                902,
                new CandidateImdbEvidence(
                    null,
                    null,
                    null,
                    null,
                    CandidateEvidenceOutcome.ConfirmedAbsence,
                    Now.AddDays(7)),
                ct);
        }

        var harness = new Harness(HttpStatusCode.OK, """{ "imdb_id": "tt0111161" }""");
        await using var db = fixture.CreateMigratedContext();

        // act
        var snapshot = await harness.Source(db, dailyLimit: 0).GetOutsideLibraryRecommendationsAsync(ct);

        // assert
        snapshot.AdmittedCards.ShouldNotBeNull();
        snapshot.AdmittedCards.Select(card => card.TmdbId).ShouldBe([902]);
        harness.TmdbHandler.Requests.ShouldBeEmpty();
        harness.OmdbHandler.GateAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task GetOutsideLibraryRecommendationsAsync_ResolvedEvidence_IsReusedByALaterScope()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedCandidatesAsync(ct);
        await using (var writeDb = fixture.CreateMigratedContext())
        {
            await new EfCandidateEvidenceStore(writeDb, new FixedTimeProvider(Now)).WriteAsync(
                902,
                Complete(),
                ct);
            writeDb.DailyRequestUsages.Add(new DailyRequestUsageRecord
            {
                UsageDate = DateOnly.FromDateTime(Now.UtcDateTime),
                RequestCount = 0,
            });
            await writeDb.SaveChangesAsync(ct);
        }

        var harness = new Harness(
            HttpStatusCode.OK,
            """{ "imdb_id": "tt0111161" }""",
            HttpStatusCode.OK,
            """
            { "Response": "True", "Title": "Candidate", "Type": "movie", "imdbID": "tt0111161", "imdbRating": "8.1", "imdbVotes": "25000" }
            """);

        // act
        await using (var first = fixture.CreateMigratedContext())
        {
            var firstSnapshot = await harness.Source(first, dailyLimit: 1).GetOutsideLibraryRecommendationsAsync(ct);
            firstSnapshot.AdmittedCards.ShouldNotBeNull();
            firstSnapshot.AdmittedCards.Select(card => card.TmdbId).ShouldBe([901, 902]);
        }

        var callsAfterFirstLoad = harness.TmdbHandler.Requests.Count;
        var sendsAfterFirstLoad = harness.OmdbHandler.GateAttempts;
        await using var second = fixture.CreateMigratedContext();
        var secondSnapshot = await harness.Source(second, dailyLimit: 1).GetOutsideLibraryRecommendationsAsync(ct);

        // assert
        secondSnapshot.AdmittedCards.ShouldNotBeNull();
        secondSnapshot.AdmittedCards.Select(card => card.TmdbId).ShouldBe([901, 902]);
        harness.TmdbHandler.Requests.Count.ShouldBe(callsAfterFirstLoad);
        harness.OmdbHandler.GateAttempts.ShouldBe(sendsAfterFirstLoad);
        callsAfterFirstLoad.ShouldBe(1);
        sendsAfterFirstLoad.ShouldBe(1);
        await using var verify = fixture.CreateMigratedContext();
        var cached = await verify.Set<CandidateEvidenceRecord>()
            .SingleAsync(record => record.TmdbId == 901, ct);
        cached.Rating.ShouldBe(8.1m);
        cached.Votes.ShouldBe(25000);
    }

    private static CandidateImdbEvidence Complete() =>
        new("tt0111161", null, 8.0m, 5000, CandidateEvidenceOutcome.Complete, Now.AddDays(30));

    private async Task SeedCandidatesAsync(CancellationToken ct)
    {
        var first = Record(1, "Seed One");
        var second = Record(2, "Seed Two");
        first.IsWatched = true;
        second.IsWatched = true;

        await using var db = fixture.CreateMigratedContext();
        db.Movies.AddRange(first, second);
        await db.SaveChangesAsync(ct);

        foreach (var (sourceId, tmdbId) in new[]
        {
            (1, 901),
            (2, 901),
            (1, 902),
            (2, 902),
        })
        {
            db.Set<MovieRecommendationRecord>().Add(new MovieRecommendationRecord
            {
                SourceMovieId = new MovieId(sourceId),
                RecommendedTmdbId = tmdbId,
                Title = $"Candidate {tmdbId}",
                ReleaseYear = 2001,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static MovieRecord Record(int id, string title) =>
        new()
        {
            Id = new MovieId(id),
            Title = title,
            LibraryPath = new LibraryPath($"C:/library/{id}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Enriched,
        };

    private sealed class Harness
    {
        private readonly RecordingHandler omdbHandler;
        private readonly RecordingHandler tmdbHandler;
        private readonly CandidateEvidenceFlightRegistry flights = new();

        public Harness(
            HttpStatusCode tmdbStatus,
            string tmdbBody,
            HttpStatusCode omdbStatus = HttpStatusCode.InternalServerError,
            string omdbBody = "")
        {
            omdbHandler = new RecordingHandler(omdbStatus, omdbBody);
            tmdbHandler = new RecordingHandler(tmdbStatus, tmdbBody);
            Omdb = new OmdbMetadataProvider(
                new HttpClient(omdbHandler) { BaseAddress = new Uri("http://localhost:9999/") },
                Options.Create(new OmdbOptions { ApiKey = "key", BaseUrl = "http://localhost:9999/" }),
                new FixedTimeProvider(Now),
                NullLogger<OmdbMetadataProvider>.Instance,
                new MetadataProviderHealthState());
            Tmdb = new TmdbMetadataProvider(
                new HttpClient(tmdbHandler) { BaseAddress = new Uri("http://localhost:9999/3/") },
                Options.Create(new TmdbOptions { Enabled = true, ApiKey = "key" }),
                Options.Create(new OmdbOptions()),
                new FixedTimeProvider(Now),
                NullLogger<TmdbMetadataProvider>.Instance,
                new SecondaryMetadataProviderHealthState());
        }

        private OmdbMetadataProvider Omdb { get; }

        private TmdbMetadataProvider Tmdb { get; }

        public RecordingHandler OmdbHandler => omdbHandler;

        public RecordingHandler TmdbHandler => tmdbHandler;

        public EfRecommendationCandidateSource Source(LamuFlixDbContext db, int dailyLimit) =>
            new(
                db,
                new EfCandidateEvidenceStore(db, new FixedTimeProvider(Now)),
                Options.Create(new RecommendationOptions()),
                new FixedTimeProvider(Now),
                new OutsideLibraryEvidenceResolver(
                    writer: new CandidateEvidenceWriter(
                        new EfCandidateEvidenceStore(db, new FixedTimeProvider(Now)),
                        new FixedTimeProvider(Now)),
                    flights: flights,
                    logger: NullLogger<OutsideLibraryEvidenceResolver>.Instance,
                    tmdb: Tmdb,
                    omdb: Omdb),
                Options.Create(new OmdbQuotaOptions { DailyLimit = dailyLimit }));
    }

    private sealed class RecordingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        public int GateAttempts { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            GateAttempts++;
            Requests.Add(request.RequestUri ?? throw new InvalidOperationException("Missing request URI."));
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
