using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class MovieLibraryPathKeyUniquenessTests(PostgresFixture fixture)
{
    [Fact]
    public async Task KeyIndex_SameExactPathWithDifferentCasing_RejectsOnTheKeyIndex()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await CreateActivatedContextAsync(cancellationToken);
        await SeedAsync(context, "Heat", "Heat (1995)/heat.mkv", cancellationToken);

        // act
        var failure = await Record.ExceptionAsync(() => SeedAsync(context, "Heat Shout", "HEAT (1995)/HEAT.MKV", cancellationToken));

        // assert
        ExpectConstraintViolation(failure, "IX_movies_library_path_key");
    }

    [Fact]
    public async Task ExactIndex_ExactDuplicatePath_RejectsWith23505OnLibraryPathIndex()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await CreateActivatedContextAsync(cancellationToken);
        await SeedAsync(context, "Heat", "Heat (1995)/heat.mkv", cancellationToken);

        // act
        var failure = await Record.ExceptionAsync(
            () => InsertRawAsync(context, "Heat Raw", "Heat (1995)/heat.mkv", "HEAT (1995)/HEAT.MKV", cancellationToken));

        // assert
        ExpectConstraintViolation(failure, "IX_movies_library_path");
    }

    [Fact]
    public async Task KeyIndex_CaseOnlyDuplicate_RejectsWith23505OnLibraryPathKeyIndex()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await CreateActivatedContextAsync(cancellationToken);
        await SeedAsync(context, "Heat", "Heat (1995)/heat.mkv", cancellationToken);

        // act
        var failure = await Record.ExceptionAsync(() => SeedAsync(context, "Heat Lower", "heat (1995)/heat.mkv", cancellationToken));

        // assert
        ExpectConstraintViolation(failure, "IX_movies_library_path_key");
    }

    [Fact]
    public async Task KeyIndex_LongSAndPlainS_RejectsWith23505OnLibraryPathKeyIndex()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await CreateActivatedContextAsync(cancellationToken);
        await SeedAsync(context, "Plain S", "stest/movie.mkv", cancellationToken);

        // act
        var failure = await Record.ExceptionAsync(() => SeedAsync(context, "Long S", "\u017Ftest/movie.mkv", cancellationToken));

        // assert
        ExpectConstraintViolation(failure, "IX_movies_library_path_key");
    }

    [Fact]
    public async Task KeyIndex_GarayCasePair_AcceptsBothRowsUnderDistinctOrdinalKeys()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await CreateActivatedContextAsync(cancellationToken);

        var collapses = string.Equals(
            "\U00010D70".ToUpperInvariant(),
            "\U00010D50",
            StringComparison.Ordinal);

        // act
        await SeedAsync(context, "Garay upper", "\U00010D50movie/movie.mkv", cancellationToken);
        var failure = await Record.ExceptionAsync(
            () => SeedAsync(context, "Garay lower", "\U00010D70movie/movie.mkv", cancellationToken));

        // assert
        if (collapses)
        {
            ExpectConstraintViolation(failure, "IX_movies_library_path_key");
            return;
        }

        failure.ShouldBeNull();
        var keys = await context.Database
            .SqlQueryRaw<string>("SELECT library_path_key AS \"Value\" FROM movies ORDER BY id")
            .ToListAsync(cancellationToken);
        keys.Count.ShouldBe(2);
        keys[0].ShouldNotBe(keys[1]);
        keys[0].ShouldBe(keys[0].ToUpperInvariant());
        keys[1].ShouldBe(keys[1].ToUpperInvariant());
    }

    [Fact]
    public async Task KeyIndex_ConcurrentCaseOnlyImports_ProduceExactlyOneWinnerAndOne23505()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await CreateActivatedContextAsync(cancellationToken);
        var connectionString = fixture.FullConnectionString(context);

        // act
        var failures = await Task.WhenAll(
            RecordExceptionAsync(() => SeedOnFreshContextAsync(connectionString, "Upper", "Race/heat.mkv", cancellationToken)),
            RecordExceptionAsync(() => SeedOnFreshContextAsync(connectionString, "Lower", "race/HEAT.mkv", cancellationToken)));

        // assert
        failures.Count(failure => failure is null).ShouldBe(1);
        var rejected = failures.Single(failure => failure is not null);
        ExpectConstraintViolation(rejected, "IX_movies_library_path_key");
        (await context.Movies.CountAsync(cancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task Column_KeyColumn_CarriesBinaryCollationAndNotNullAfterActivation()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await CreateActivatedContextAsync(cancellationToken);

        // act
        var state = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT (column_state.is_nullable || '|' || COALESCE((SELECT coll.collname FROM pg_collation AS coll
                    WHERE coll.oid = attribute.attcollation), '')) AS "Value"
                FROM information_schema.columns AS column_state
                JOIN pg_attribute AS attribute
                  ON attribute.attname = column_state.column_name
                 AND attribute.attnum > 0
                 AND NOT attribute.attisdropped
                JOIN pg_class AS relation
                  ON relation.oid = attribute.attrelid AND relation.relname = 'movies'
                WHERE column_state.table_schema = 'public'
                  AND column_state.table_name = 'movies'
                  AND column_state.column_name = 'library_path_key'
                """)
            .SingleAsync(cancellationToken);

        // assert
        state.ShouldBe("NO|C");
        var definitions = await context.Database
            .SqlQueryRaw<string>(
                "SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'movies'")
            .ToListAsync(cancellationToken);
        definitions.ShouldContain(definition => definition.Contains("IX_movies_library_path", StringComparison.Ordinal)
            && definition.Contains("UNIQUE", StringComparison.Ordinal)
            && definition.Contains("(library_path)", StringComparison.Ordinal));
        definitions.ShouldContain(definition => definition.Contains("IX_movies_library_path_key", StringComparison.Ordinal)
            && definition.Contains("UNIQUE", StringComparison.Ordinal)
            && definition.Contains("(library_path_key)", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Heat (1995)/heat.mkv")]
    [InlineData("heat/movie.mkv")]
    public async Task SaveChangesChokePoint_EveryTrackedWriter_DerivesWholeStringUpperInvariantKey(string path)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await CreateActivatedContextAsync(cancellationToken);

        // act
        var record = new MovieRecord
        {
            Title = "Choke point",
            LibraryPath = new LibraryPath(path),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending,
            LibraryPathKey = "STALE",
        };
        context.Movies.Add(record);
        await context.SaveChangesAsync(cancellationToken);

        // assert
        record.LibraryPathKey.ShouldBe(path.ToUpperInvariant());
        var stored = await context.Database
            .SqlQuery<string>($"SELECT library_path_key AS \"Value\" FROM movies WHERE id = {RequiredId(record)}")
            .SingleAsync(cancellationToken);
        stored.ShouldBe(path.ToUpperInvariant());
    }

    [Fact]
    public async Task SaveChangesChokePoint_SynchronousSaveChanges_AlsoDerivesTheKey()
    {
        // arrange
        await using var context = await CreateActivatedContextAsync(TestContext.Current.CancellationToken);

        // act
        var record = new MovieRecord
        {
            Title = "Sync writer",
            LibraryPath = new LibraryPath("Sync/Writer.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending,
        };
        context.Movies.Add(record);
        // ReSharper disable once MethodHasAsyncOverload - the sync SaveChanges overload is the subject under test
        var written = context.SaveChanges();

        // assert
        written.ShouldBe(1);
        record.LibraryPathKey.ShouldBe("SYNC/WRITER.MKV");
    }

    [Fact]
    public async Task SaveChangesChokePoint_PathMutatedOnTrackedRow_RefreshesTheKey()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await CreateActivatedContextAsync(cancellationToken);
        var record = await SeedAsync(context, "Mutable", "First/Path.mkv", cancellationToken);
        record.LibraryPathKey.ShouldBe("FIRST/PATH.MKV");

        // act
        record.LibraryPath = new LibraryPath("Second/Path.mkv");
        context.Movies.Update(record);
        await context.SaveChangesAsync(cancellationToken);

        // assert
        record.LibraryPathKey.ShouldBe("SECOND/PATH.MKV");
        var stored = await context.Database
            .SqlQuery<string>($"SELECT library_path_key AS \"Value\" FROM movies WHERE id = {RequiredId(record)}")
            .SingleAsync(cancellationToken);
        stored.ShouldBe("SECOND/PATH.MKV");
    }

    [Fact]
    public async Task SaveChangesChokePoint_UnmodifiedRows_LeaveTheStoredKeyAlone()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await CreateActivatedContextAsync(cancellationToken);
        var record = await SeedAsync(context, "Untouched", "Untouched/path.mkv", cancellationToken);
        context.ChangeTracker.Clear();

        // act
        var reread = await context.Movies.SingleAsync(movie => movie.Id == record.Id, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // assert
        reread.LibraryPathKey.ShouldBe("UNTOUCHED/PATH.MKV");
    }

    private static async Task<Exception?> RecordExceptionAsync(Func<Task> action) => await Record.ExceptionAsync(action);

    private static int RequiredId(MovieRecord record) =>
        record.Id?.Value ?? throw new InvalidOperationException("Persisted movie is missing an id.");

    private static void ExpectConstraintViolation(Exception? failure, string constraintName)
    {
        var postgres = failure.ShouldNotBeNull();
        var unwrapped = Find(postgres).ShouldNotBeNull();
        unwrapped.SqlState.ShouldBe("23505");
        unwrapped.ConstraintName.ShouldBe(constraintName);
    }

    private static PostgresException? Find(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }

    private async Task<LamuFlixDbContext> CreateActivatedContextAsync(CancellationToken cancellationToken)
    {
        var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        await PostgresFixture.ActivateLibraryPathKeyAsync(context, cancellationToken);
        return context;
    }

    private static async Task<MovieRecord> SeedAsync(
        LamuFlixDbContext context,
        string title,
        string path,
        CancellationToken cancellationToken)
    {
        var record = new MovieRecord
        {
            Title = title,
            LibraryPath = new LibraryPath(path),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending,
        };
        context.Movies.Add(record);
        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
        return record;
    }

    private static Task SeedOnFreshContextAsync(
        string connectionString,
        string title,
        string path,
        CancellationToken cancellationToken) =>
        SeedOnFreshContextCoreAsync(LamuFlixDbContextFactory.OpenContext(connectionString), title, path, cancellationToken);

    private static async Task SeedOnFreshContextCoreAsync(
        LamuFlixDbContext context,
        string title,
        string path,
        CancellationToken cancellationToken)
    {
        await using (context)
        {
            await SeedAsync(context, title, path, cancellationToken);
        }
    }

    private static Task InsertRawAsync(
        LamuFlixDbContext context,
        string title,
        string path,
        string key,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlRawAsync(
            "INSERT INTO movies (title, library_path, library_path_key, format, status, enrichment_attempts, is_in_watchlist, added_at) VALUES ({0}, {1}, {2}, 'mkv', 0, 0, false, now())",
            [title, path, key],
            cancellationToken);

}


