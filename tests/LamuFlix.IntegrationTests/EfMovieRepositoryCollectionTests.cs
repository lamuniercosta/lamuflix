using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(RepositoryMutationCollection))]
public sealed class EfMovieRepositoryCollectionTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChanges_CollectionChanged_ReplacesMembershipAndRetainsPriorRow()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Replace", "collection-replace", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(MetadataWithCollection("Replace", "tt0000101", 101, "First Franchise"), Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(MetadataWithCollection("Replace", "tt0000101", 202, "Second Franchise"), Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.CollectionId.ShouldBe(202);
        (await verify.Collections.AsNoTracking().OrderBy(collection => collection.Id).ToListAsync(ct))
            .Select(collection => (collection.Id, collection.Name))
            .ShouldBe([(101, "First Franchise"), (202, "Second Franchise")]);
    }

    [Fact]
    public async Task SaveChanges_MetadataWithoutCollection_ClearsMembershipAndRetainsCollectionRow()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Fallback", "collection-fallback", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(MetadataWithCollection("Fallback", "tt0000102", 303, "Fallback Franchise"), Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(MetadataWithoutCollection("Fallback", "tt0000102"), Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.CollectionId.ShouldBeNull();
        (await verify.Collections.AsNoTracking().Select(collection => collection.Id).ToArrayAsync(ct)).ShouldBe([303]);
    }

    [Fact]
    public async Task SaveChanges_FailedEnrichment_PreservesMetadataAndMembership()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Failure", "collection-failure", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(MetadataWithCollection("Failure", "tt0000103", 404, "Failure Franchise"), Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkFailed(EnrichmentFailureCategory.ProviderUnavailable, Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.CollectionId.ShouldBe(404);
        record.MetadataTitle.ShouldBe("Failure");
        record.ImdbId.ShouldBe(new ImdbId("tt0000103"));
        (await verify.Collections.AsNoTracking().Select(collection => collection.Id).ToArrayAsync(ct)).ShouldBe([404]);
    }

    [Fact]
    public async Task TryDeleteAsync_MovieWithCollection_RemovesOnlyTheMovie()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Cascade", "collection-cascade", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(MetadataWithCollection("Cascade", "tt0000104", 505, "Cascade Franchise"), Now);
        await repository.SaveChangesAsync(ct);

        // act
        var removed = await repository.TryDeleteAsync(id, ct);

        // assert
        removed.ShouldBeTrue();
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Movies.AsNoTracking().CountAsync(item => item.Id == id, ct)).ShouldBe(0);
        (await verify.Collections.AsNoTracking().Select(collection => collection.Id).ToArrayAsync(ct)).ShouldBe([505]);
    }

    [Fact]
    public async Task ConcurrentEnrichment_SameNewCollectionId_CreatesOneRowAndBothMembersWithoutConflict()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var gate = new CollectionInsertGateInterceptor();
        await using var contextA = fixture.CreateMigratedContext();
        await using var contextB = OpenInstrumentedContext(gate);
        var repositoryA = Repository(contextA);
        var repositoryB = Repository(contextB);
        var movieA = Movie.Create(
            await repositoryA.NextIdentityAsync(ct),
            "Alpha", new LibraryPath("C:/library/collection-race-alpha.mkv"), new MediaFormat("mkv"));
        var movieB = Movie.Create(
            await repositoryB.NextIdentityAsync(ct),
            "Bravo", new LibraryPath("C:/library/collection-race-bravo.mkv"), new MediaFormat("mkv"));
        await repositoryA.AddAsync(movieA, ct);
        await repositoryB.AddAsync(movieB, ct);
        movieA.MarkEnriched(MetadataWithCollection("Alpha", "tt0000105", 606, "Raced Franchise"), Now);
        movieB.MarkEnriched(MetadataWithCollection("Bravo", "tt0000106", 606, "Raced Franchise"), Now);

        // act
        var saveB = repositoryB.SaveChangesAsync(ct);
        await gate.Arrived.WaitAsync(TimeSpan.FromSeconds(30), ct);
        await repositoryA.SaveChangesAsync(ct);
        gate.Release();
        await saveB;

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var collection = await verify.Collections.AsNoTracking().SingleAsync(ct);
        collection.Id.ShouldBe(606);
        collection.Name.ShouldBe("Raced Franchise");
        (await verify.Movies.AsNoTracking().Select(item => item.CollectionId).ToArrayAsync(ct))
            .ShouldBe([606, 606], ignoreOrder: true);
    }

    [Fact]
    public async Task MigrateAsync_CollectionsTable_UsesNaturalPrimaryKeyAndCollectionIndex()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(ct);

        // act
        await context.Database.MigrateAsync(ct);

        // assert
        (await QueryStringsAsync(
                context,
                """
                SELECT column_name
                FROM information_schema.table_constraints AS constraints
                JOIN information_schema.key_column_usage AS keys
                  ON constraints.constraint_name = keys.constraint_name
                 AND constraints.table_schema = keys.table_schema
                WHERE constraints.constraint_type = 'PRIMARY KEY'
                  AND constraints.table_schema = 'public'
                  AND constraints.table_name = 'collections'
                """,
                ct))
            .ShouldBe(["id"]);
        (await QueryStringsAsync(
                context,
                """
                SELECT is_identity
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'collections' AND column_name = 'id'
                """,
                ct))
            .ShouldBe(["NO"]);
        var indexes = await QueryStringsAsync(
            context,
            """
            SELECT indexdef
            FROM pg_indexes
            WHERE schemaname = 'public' AND tablename = 'movies' AND indexname = 'IX_movies_collection_id'
            """,
            ct);
        indexes.Count.ShouldBe(1);
        indexes[0].ShouldContain("collection_id");
        indexes[0].ShouldNotContain("UNIQUE");
        (await QueryStringsAsync(
                context,
                """
                SELECT delete_rule
                FROM information_schema.referential_constraints
                WHERE constraint_schema = 'public' AND constraint_name = 'FK_movies_collections_collection_id'
                """,
                ct))
            .ShouldBe(["RESTRICT"]);
    }

    [Fact]
    public async Task MigrateAsync_LegacyMovieRow_RemainsWithoutCollectionMembership()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO movies (title, library_path, format, status, enrichment_attempts, is_in_watchlist) VALUES ('Legacy', 'C:/library/collection-legacy.mkv', 'mkv', 0, 0, false)",
            ct);

        // act
        var record = await db.Movies.AsNoTracking().SingleAsync(item => item.Title == "Legacy", ct);

        // assert
        record.CollectionId.ShouldBeNull();
        (await db.Collections.CountAsync(ct)).ShouldBe(0);
    }

    private static MovieMetadata MetadataWithCollection(
        string title, string imdbId, int collectionId, string collectionName) =>
        new(
            title,
            "Full synopsis",
            new ReleaseYear(2024, Now),
            new Runtime(121),
            new ImdbRating(8.1m),
            new ImdbId(imdbId),
            "https://example.test/poster.jpg",
            null,
            82,
            [],
            [],
            [],
            [],
            collectionId,
            collectionName);

    private static MovieMetadata MetadataWithoutCollection(string title, string imdbId) =>
        new(
            title,
            "Full synopsis",
            new ReleaseYear(2024, Now),
            new Runtime(121),
            new ImdbRating(8.1m),
            new ImdbId(imdbId),
            "https://example.test/poster.jpg");

    private static async Task<MovieId> AddMovieAsync(
        EfMovieRepository repository, string title, string suffix, CancellationToken ct)
    {
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(
            Movie.Create(id, title, new LibraryPath($"C:/library/{suffix}.mkv"), new MediaFormat("mkv")), ct);
        await repository.SaveChangesAsync(ct);
        return id;
    }

    private static async Task<List<string>> QueryStringsAsync(
        LamuFlixDbContext context, string sql, CancellationToken ct) =>
        await context.Database.SqlQueryRaw<string>(sql).ToListAsync(ct);

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

    private sealed class CollectionInsertGateInterceptor : DbCommandInterceptor
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
            if (command.CommandText.Contains("INSERT INTO collections", StringComparison.Ordinal)
                || command.CommandText.Contains("INSERT INTO \"collections\"", StringComparison.Ordinal))
            {
                arrived.TrySetResult();
                await released.Task.WaitAsync(cancellationToken);
            }
        }
    }
}
