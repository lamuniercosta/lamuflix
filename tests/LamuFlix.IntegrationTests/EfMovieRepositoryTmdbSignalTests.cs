using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(RepositoryMutationCollection))]
public sealed class EfMovieRepositoryTmdbSignalTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChanges_AvailableSignals_PersistsKeywordRowsAndProvenanceJoins()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Available", "signals-available", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkEnriched(
            Metadata(
                "Available",
                "tt0000201",
                Signals(keywords: Keywords((101, "Heist"), (202, "Time Travel")), fullCast: Cast("Ada Actor", "Bo Actor"))),
            Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Keywords.AsNoTracking().OrderBy(keyword => keyword.Id).ToListAsync(ct))
            .Select(keyword => (keyword.Id, keyword.Name))
            .ShouldBe([(101, "Heist"), (202, "Time Travel")]);
        var record = await LoadTrackedMovieAsync(verify, id, ct);
        record.Keywords.Select(keyword => keyword.Id).Order().ToArray().ShouldBe([101, 202]);
        record.FullCast.Select(actor => actor.Name).Order().ToArray()
            .ShouldBe(["Ada Actor", "Bo Actor"]);
    }

    [Fact]
    public async Task SaveChanges_FullCastSignal_ReusesBaseActorIdentity()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Reuse", "signals-reuse", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkEnriched(
            Metadata(
                "Reuse",
                "tt0000202",
                Signals(fullCast: Cast("Ada Actor", "Bo Actor")),
                actors: ["Ada Actor"]),
            Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Actors.AsNoTracking().Select(actor => actor.Name).ToListAsync(ct))
            .Order()
            .ShouldBe(["Ada Actor", "Bo Actor"]);
        var record = await LoadTrackedMovieAsync(verify, id, ct);
        record.Actors.Select(actor => actor.Name).ShouldBe(["Ada Actor"]);
        record.FullCast.Select(actor => actor.Name).Order().ToArray()
            .ShouldBe(["Ada Actor", "Bo Actor"]);
    }

    [Fact]
    public async Task SaveChanges_RepeatedAvailableSignals_ReplacesMembershipAndRetainsPriorKeywordRow()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Replace", "signals-replace", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            Metadata("Replace", "tt0000203", Signals(keywords: Keywords((101, "Heist")))), Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(
            Metadata(
                "Replace",
                "tt0000203",
                Signals(keywords: Keywords((303, "Sailing")), fullCast: Cast("Cy Actor"))),
            Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Keywords.AsNoTracking().Select(keyword => keyword.Id).Order().ToListAsync(ct))
            .ShouldBe([101, 303]);
        var record = await LoadTrackedMovieAsync(verify, id, ct);
        record.Keywords.Select(keyword => keyword.Id).ToArray().ShouldBe([303]);
        record.FullCast.Select(actor => actor.Name).ToArray().ShouldBe(["Cy Actor"]);
    }

    [Fact]
    public async Task SaveChanges_KnownEmptySignals_ClearsMembershipsAndRetainsLookups()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Empty", "signals-empty", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            Metadata(
                "Empty",
                "tt0000204",
                Signals(keywords: Keywords((101, "Heist")), fullCast: Cast("Ada Actor"))),
            Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(
            Metadata(
                "Empty",
                "tt0000204",
                Signals(
                    keywordAvailability: SignalAvailability.KnownEmpty,
                    fullCastAvailability: SignalAvailability.KnownEmpty)),
            Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadTrackedMovieAsync(verify, id, ct);
        record.Keywords.ShouldBeEmpty();
        record.FullCast.ShouldBeEmpty();
        (await verify.Keywords.CountAsync(ct)).ShouldBe(1);
        (await verify.Actors.CountAsync(ct)).ShouldBe(1);
    }

    [Fact]
    public async Task SaveChanges_UnavailableSignals_PreservesMembershipsWhileUpdatingOtherMetadata()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Unavailable", "signals-unavailable", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            Metadata(
                "Unavailable",
                "tt0000205",
                Signals(keywords: Keywords((101, "Heist")), fullCast: Cast("Ada Actor"))),
            Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(
            Metadata("Unavailable", "tt0000205", Signals(), "Updated synopsis"),
            Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadTrackedMovieAsync(verify, id, ct);
        record.Plot.ShouldBe("Updated synopsis");
        record.Keywords.Select(keyword => keyword.Id).ToArray().ShouldBe([101]);
        record.FullCast.Select(actor => actor.Name).ToArray().ShouldBe(["Ada Actor"]);
    }

    [Fact]
    public async Task SaveChanges_DisabledSignals_PreserveMemberships()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Disabled", "signals-disabled", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            Metadata(
                "Disabled",
                "tt0000206",
                Signals(keywords: Keywords((101, "Heist")), fullCast: Cast("Ada Actor"))),
            Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(Metadata("Disabled", "tt0000206", null, "Disabled synopsis"), Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadTrackedMovieAsync(verify, id, ct);
        record.Plot.ShouldBe("Disabled synopsis");
        record.Keywords.Select(keyword => keyword.Id).ToArray().ShouldBe([101]);
        record.FullCast.Select(actor => actor.Name).ToArray().ShouldBe(["Ada Actor"]);
    }

    [Fact]
    public async Task SaveChanges_ReloadedThenResaved_DoesNotWipeSignalMemberships()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var seedDb = fixture.CreateMigratedContext();
        var seedRepository = Repository(seedDb);
        var id = await AddMovieAsync(seedRepository, "Reload", "signals-reload", ct);
        var seeded = await seedRepository.GetAsync(id, ct);
        seeded.ShouldNotBeNull();
        seeded.MarkEnriched(
            Metadata(
                "Reload",
                "tt0000207",
                Signals(
                    keywords: Keywords((101, "Heist"), (202, "Time Travel")),
                    fullCast: Cast("Ada Actor", "Bo Actor"))),
            Now);
        await seedRepository.SaveChangesAsync(ct);

        // act
        await using var reloadDb = fixture.CreateMigratedContext();
        var reloadRepository = Repository(reloadDb);
        var reloaded = await reloadRepository.GetAsync(id, ct);
        reloaded.ShouldNotBeNull();
        reloaded.RequestEnrichment();
        reloaded.MarkEnriched(
            Metadata(
                "Reload",
                "tt0000207",
                Signals(
                    keywords: Keywords((101, "Heist"), (202, "Time Travel")),
                    fullCast: Cast("Ada Actor", "Bo Actor")),
                "Reloaded synopsis"),
            Now.AddMinutes(1));
        await reloadRepository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadTrackedMovieAsync(verify, id, ct);
        record.Plot.ShouldBe("Reloaded synopsis");
        record.Keywords.Select(keyword => keyword.Id).Order().ToArray().ShouldBe([101, 202]);
        record.FullCast.Select(actor => actor.Name).Order().ToArray()
            .ShouldBe(["Ada Actor", "Bo Actor"]);
        (await verify.Keywords.CountAsync(ct)).ShouldBe(2);
    }

    [Fact]
    public async Task SaveChanges_MixedIndependentSignalStates_ApplyPerSignal()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Mixed", "signals-mixed", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            Metadata(
                "Mixed",
                "tt0000208",
                Signals(
                    keywords: Keywords((101, "Heist")),
                    fullCast: Cast("Ada Actor"),
                    collectionAvailability: SignalAvailability.Available),
                collectionId: 909,
                collectionName: "Mixed Franchise"),
            Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(
            Metadata(
                "Mixed",
                "tt0000208",
                Signals(
                    keywords: Keywords((505, "Noir")),
                    fullCastAvailability: SignalAvailability.KnownEmpty)),
            Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadTrackedMovieAsync(verify, id, ct);
        record.Keywords.Select(keyword => keyword.Id).ToArray().ShouldBe([505]);
        record.FullCast.ShouldBeEmpty();
        record.CollectionId.ShouldBe(909);
    }

    [Fact]
    public async Task ConcurrentEnrichment_SameNewKeywordId_CreatesOneRowAndBothMembers()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var gate = new KeywordInsertGateInterceptor();
        await using var contextA = fixture.CreateMigratedContext();
        await using var contextB = OpenInstrumentedContext(gate);
        var repositoryA = Repository(contextA);
        var repositoryB = Repository(contextB);
        var movieA = Movie.Create(
            await repositoryA.NextIdentityAsync(ct),
            "Alpha Keyword", new LibraryPath("C:/library/signals-keyword-alpha.mkv"), new MediaFormat("mkv"));
        var movieB = Movie.Create(
            await repositoryB.NextIdentityAsync(ct),
            "Bravo Keyword", new LibraryPath("C:/library/signals-keyword-bravo.mkv"), new MediaFormat("mkv"));
        await repositoryA.AddAsync(movieA, ct);
        await repositoryB.AddAsync(movieB, ct);
        movieA.MarkEnriched(
            Metadata("Alpha Keyword", "tt0000209", Signals(keywords: Keywords((606, "Raced")))), Now);
        movieB.MarkEnriched(
            Metadata("Bravo Keyword", "tt0000210", Signals(keywords: Keywords((606, "Raced")))), Now);

        // act
        var saveB = repositoryB.SaveChangesAsync(ct);
        await gate.Arrived.WaitAsync(TimeSpan.FromSeconds(30), ct);
        var saveA = repositoryA.SaveChangesAsync(ct);
        gate.Release();
        await Task.WhenAll(saveA, saveB);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var keyword = await verify.Keywords.AsNoTracking().SingleAsync(ct);
        keyword.Id.ShouldBe(606);
        keyword.Name.ShouldBe("Raced");
        var members = await verify.Movies.AsNoTracking()
            .Include(movie => movie.Keywords)
            .OrderBy(movie => movie.Title)
            .ToListAsync(ct);
        members.Select(movie => movie.Keywords.Select(item => item.Id).Single()).ShouldBe([606, 606]);
    }

    [Fact]
    public async Task SaveChanges_FailedSignalWrite_RollsBackTheEntireSave()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var interceptor = new FailingJoinInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Rollback", "signals-rollback", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            Metadata(
                "Rollback",
                "tt0000211",
                Signals(
                    keywords: Keywords((707, "Rollback"), (708, "Undone")),
                    fullCast: Cast("Ada Actor"))),
            Now);

        // act
        var failure = await Should.ThrowAsync<DbUpdateException>(
            () => repository.SaveChangesAsync(ct));

        // assert
        failure.InnerException.ShouldBeOfType<InvalidOperationException>()
            .Message.ShouldContain("movie_keywords");
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Keywords.CountAsync(ct)).ShouldBe(0);
        (await verify.Actors.CountAsync(ct)).ShouldBe(0);
        var record = await LoadTrackedMovieAsync(verify, id, ct);
        record.Keywords.ShouldBeEmpty();
        record.FullCast.ShouldBeEmpty();
        record.MetadataTitle.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_ClearMetadata_ClearsSignalMembershipsAndRetainsLookups()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Clear", "signals-clear", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            Metadata(
                "Clear",
                "tt0000212",
                Signals(keywords: Keywords((101, "Heist")), fullCast: Cast("Ada Actor"))),
            Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.ClearMetadata();
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadTrackedMovieAsync(verify, id, ct);
        record.Keywords.ShouldBeEmpty();
        record.FullCast.ShouldBeEmpty();
        (await verify.Keywords.CountAsync(ct)).ShouldBe(1);
        (await verify.Actors.CountAsync(ct)).ShouldBe(1);
    }

    private static MovieKeyword[] Keywords(params (int Id, string Name)[] keywords) =>
        [.. keywords.Select(keyword => new MovieKeyword(keyword.Id, keyword.Name))];

    private static string[] Cast(params string[] actors) => actors;

    private static MovieSignalSnapshot Signals(
        MovieKeyword[]? keywords = null,
        string[]? fullCast = null,
        SignalAvailability? keywordAvailability = null,
        SignalAvailability? fullCastAvailability = null,
        SignalAvailability collectionAvailability = SignalAvailability.Unavailable) =>
        new(
            keywordAvailability ?? (keywords is null ? SignalAvailability.Unavailable : SignalAvailability.Available),
            keywords,
            fullCastAvailability ?? (fullCast is null ? SignalAvailability.Unavailable : SignalAvailability.Available),
            fullCast,
            collectionAvailability);

    private static MovieMetadata Metadata(
        string title,
        string imdbId,
        MovieSignalSnapshot? signals,
        string synopsis = "Full synopsis",
        IEnumerable<string>? actors = null,
        int? collectionId = null,
        string? collectionName = null) =>
        new(
            title,
            synopsis,
            new ReleaseYear(2024, Now),
            new Runtime(121),
            new ImdbRating(8.1m),
            new ImdbId(imdbId),
            "https://example.test/poster.jpg",
            null,
            82,
            [],
            actors,
            [],
            [],
            collectionId,
            collectionName,
            signals);

    private static Task<MovieRecord> LoadTrackedMovieAsync(
        LamuFlixDbContext context,
        MovieId id,
        CancellationToken ct) =>
        context.Movies
            .AsNoTracking()
            .Include(movie => movie.Keywords)
            .Include(movie => movie.FullCast)
            .Include(movie => movie.Actors)
            .SingleAsync(movie => movie.Id == id, ct);

    private static async Task<MovieId> AddMovieAsync(
        EfMovieRepository repository, string title, string suffix, CancellationToken ct)
    {
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(
            Movie.Create(id, title, new LibraryPath($"C:/library/{suffix}.mkv"), new MediaFormat("mkv")), ct);
        await repository.SaveChangesAsync(ct);
        return id;
    }

    private LamuFlixDbContext OpenInstrumentedContext(DbCommandInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(fixture.ConnectionString, o => o.UseVector())
            .AddInterceptors(interceptor)
            .Options;
        return new LamuFlixDbContext(options);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(
            db,
            new FixedTimeProvider(Now),
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);

    private sealed class KeywordInsertGateInterceptor : DbCommandInterceptor
    {
        private readonly TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Arrived => arrived.Task;

        public void Release() => released.TrySetResult();

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await GateAsync(command, cancellationToken);
            return result;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await GateAsync(command, cancellationToken);
            return result;
        }

        private async Task GateAsync(DbCommand command, CancellationToken cancellationToken)
        {
            if (command.CommandText.Contains("INSERT INTO keywords", StringComparison.Ordinal)
                || command.CommandText.Contains("INSERT INTO \"keywords\"", StringComparison.Ordinal))
            {
                arrived.TrySetResult();
                await released.Task.WaitAsync(cancellationToken);
            }
        }
    }

    private sealed class FailingJoinInterceptor : DbCommandInterceptor
    {
        private bool failed;

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            FailWhenJoinInsert(command);
            return new ValueTask<InterceptionResult<int>>(result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            FailWhenJoinInsert(command);
            return new ValueTask<InterceptionResult<DbDataReader>>(result);
        }

        private void FailWhenJoinInsert(DbCommand command)
        {
            if (failed)
            {
                return;
            }

            if (command.CommandText.Contains("INSERT INTO movie_keywords", StringComparison.Ordinal)
                || command.CommandText.Contains("INSERT INTO \"movie_keywords\"", StringComparison.Ordinal))
            {
                failed = true;
                throw new InvalidOperationException("Injected failure while inserting into movie_keywords.");
            }
        }
    }
}
