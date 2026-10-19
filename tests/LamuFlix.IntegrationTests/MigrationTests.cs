using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class MigrationTests(PostgresFixture fixture)
{
    private static readonly string[] ApplicationTables =
    [
        "movies",
        "actors",
        "directors",
        "genres",
        "writers",
        "movie_actors",
        "movie_directors",
        "movie_genres",
        "movie_writers",
        "playback_events"
    ];

    private static readonly string[] NullableMoviesColumns =
    [
        "metadata_title",
        "runtime_minutes",
        "release_year",
        "imdb_rating",
        "imdb_id",
        "rotten_tomatoes_rating",
        "meta_score",
        "plot",
        "poster_url",
        "enriched_at",
        "enrichment_failure_category",
        "last_attempt_at",
        "parsed_release_year"
    ];

    private static readonly Regex SnakeCase = new(
        "^[a-z][a-z0-9]*(_[a-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // Legacy rows are inserted with SQL that names only the columns the older schema has, because EF's
    // INSERT for the current model also writes parsed_release_year, which does not exist before that migration.
    private static async Task<int> InsertLegacyMovieAsync(
        DbContext context,
        string title,
        string libraryPath,
        CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO movies (title, library_path, format, is_in_watchlist, status, enrichment_attempts) VALUES ({title}, {libraryPath}, 'mkv', false, {(int)EnrichmentStatus.Pending}, 0)",
            cancellationToken);
        return await context.Database.SqlQuery<int>($"SELECT id AS \"Value\" FROM movies WHERE title = {title}")
            .SingleAsync(cancellationToken);
    }

    [Fact]
    public async Task MigrateAsync_EmptyDatabase_AppliesMigrationsEnablesVectorAndHasNoPendingModelChanges()
    {
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(TestContext.Current.CancellationToken);

        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var cancellationToken = TestContext.Current.CancellationToken;
        var migrations = context.Database.GetMigrations().ToArray();
        migrations.Length.ShouldBe(5);
        migrations[0].ShouldEndWith("_Initial");
        migrations[1].ShouldEndWith("_AddMovieWriters");
        migrations[2].ShouldEndWith("_EnableVector");
        migrations[3].ShouldEndWith("_AddPlaybackEvents");
        migrations[4].ShouldEndWith("_AddParsedReleaseYear");
        var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        applied.ShouldBe(migrations);
        context.Database.HasPendingModelChanges().ShouldBeFalse();
        (await new PostgresCatalog(context).ListPostgresExtensionsAsync(cancellationToken)).ShouldContain("vector");
    }

    [Fact]
    public async Task MigrateAsync_FromAddMovieWritersWithExistingRows_UpgradesToEnableVectorAndKeepsRowsAndCatalog()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261016195556_AddMovieWriters", cancellationToken);
        await InsertLegacyMovieAsync(context, "Upgrade Survivor", "C:/library/upgrade-survivor.mkv", cancellationToken);

        await migrator.MigrateAsync(null, cancellationToken);

        var applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        applied.ShouldBe(context.Database.GetMigrations());
        applied[^1].ShouldEndWith("_AddParsedReleaseYear");
        (await context.Movies.SingleAsync(cancellationToken)).Title.ShouldBe("Upgrade Survivor");
        await AssertPublicTablesAsync(new PostgresCatalog(context), cancellationToken);
        (await new PostgresCatalog(context).ListPostgresExtensionsAsync(cancellationToken)).ShouldContain("vector");
        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateAsync_EnableVectorRolledBack_ReturnsToAddMovieWritersAndReapplies()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(null, cancellationToken);

        await migrator.MigrateAsync("20261016195556_AddMovieWriters", cancellationToken);

        var rolledBack = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        rolledBack.Length.ShouldBe(2);
        rolledBack[0].ShouldEndWith("_Initial");
        rolledBack[1].ShouldEndWith("_AddMovieWriters");

        await migrator.MigrateAsync(null, cancellationToken);

        (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Last().ShouldEndWith("_AddParsedReleaseYear");
    }

    [Fact]
    public async Task MigrateAsync_FromInitialWithExistingMovie_AddsWriterTablesWithoutFabricatingWriters()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260930153520_Initial", cancellationToken);
        await InsertLegacyMovieAsync(context, "Legacy", "C:/library/legacy.mkv", cancellationToken);

        await migrator.MigrateAsync(null, cancellationToken);

        (await context.Movies.CountAsync(cancellationToken)).ShouldBe(1);
        (await context.Writers.CountAsync(cancellationToken)).ShouldBe(0);
        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateAsync_SecondMigrationRolledBack_DropsWriterTablesAndKeepsExistingData()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(null, cancellationToken);

        var movie = new MovieRecord
        {
            Title = "Survivor",
            LibraryPath = new LibraryPath("C:/library/survivor.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending
        };
        movie.Writers.Add(new WriterRecord { Name = "Rollback Writer" });
        context.Movies.Add(movie);
        await context.SaveChangesAsync(cancellationToken);

        await migrator.MigrateAsync("20260930153520_Initial", cancellationToken);

        var publicTables = await new PostgresCatalog(context).ListPublicTablesAsync(cancellationToken);
        publicTables.ShouldContain("movies");
        publicTables.ShouldNotContain("writers");
        publicTables.ShouldNotContain("movie_writers");
        (await context.Movies.CountAsync(cancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task MigrateAsync_AddPlaybackEventsUpgradeDowngradeReapply_PreservesMoviesAndDropsEvents()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261016195556_AddMovieWriters", cancellationToken);
        var movieId = new MovieId(await InsertLegacyMovieAsync(context, "Upgrade Survivor", "C:/library/upgrade-survivor.mkv", cancellationToken));

        await migrator.MigrateAsync(null, cancellationToken);

        var catalog = new PostgresCatalog(context);
        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldContain("playback_events");
        (await context.Movies.CountAsync(cancellationToken)).ShouldBe(1);
        await AssertPlaybackEventsColumnsAsync(catalog, cancellationToken);
        await AssertPlaybackEventsForeignKeyAsync(catalog, cancellationToken);
        await AssertPlaybackEventsIndexAsync(catalog, cancellationToken);
        context.PlaybackEvents.Add(new PlaybackEventRecord
        {
            MovieId = movieId,
            PlayedAt = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero)
        });
        await context.SaveChangesAsync(cancellationToken);

        await migrator.MigrateAsync("20261016195556_AddMovieWriters", cancellationToken);

        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldNotContain("playback_events");
        (await context.Movies.CountAsync(cancellationToken)).ShouldBe(1);

        await migrator.MigrateAsync(null, cancellationToken);

        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldContain("playback_events");
        (await context.Movies.CountAsync(cancellationToken)).ShouldBe(1);
        (await context.PlaybackEvents.CountAsync(cancellationToken)).ShouldBe(0);
        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateAsync_EmptyDatabase_MatchesPostgresCatalogContract()
    {
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(TestContext.Current.CancellationToken);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var catalog = new PostgresCatalog(context);
        var cancellationToken = TestContext.Current.CancellationToken;

        await AssertPublicTablesAsync(catalog, cancellationToken);
        await AssertColumnsAreSnakeCaseAsync(catalog, cancellationToken);
        await AssertMoviesNullabilityAsync(catalog, cancellationToken);
        await AssertJoinPrimaryKeysAsync(catalog, cancellationToken);
        await AssertMovieIndexDefinitionsAsync(catalog, cancellationToken);
    }

    private static async Task AssertPublicTablesAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var publicTables = await catalog.ListPublicTablesAsync(cancellationToken);
        publicTables.ShouldContain("__EFMigrationsHistory");
        publicTables.Except(["__EFMigrationsHistory"]).ShouldBe(ApplicationTables, ignoreOrder: true);
    }

    private static async Task AssertColumnsAreSnakeCaseAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        foreach (var table in ApplicationTables)
        {
            await AssertTableColumnsAreSnakeCaseAsync(catalog, table, cancellationToken);
        }
    }

    private static async Task AssertTableColumnsAreSnakeCaseAsync(PostgresCatalog catalog, string table, CancellationToken cancellationToken)
    {
        var columns = await catalog.ListColumnsAsync(table, cancellationToken);
        columns.ShouldNotBeEmpty();
        foreach (var column in columns)
        {
            SnakeCase.IsMatch(column).ShouldBeTrue($"Column {table}.{column} must be snake_case.");
        }
    }

    private static async Task AssertMoviesNullabilityAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var nullability = await catalog.ListMoviesNullabilityAsync(cancellationToken);
        foreach (var column in NullableMoviesColumns)
        {
            nullability[column].ShouldBeTrue($"Column movies.{column} must be nullable.");
        }

        foreach (var (column, isNullable) in nullability)
        {
            AssertRequiredMoviesColumn(column, isNullable);
        }
    }

    private static void AssertRequiredMoviesColumn(string column, bool isNullable)
    {
        if (NullableMoviesColumns.Contains(column, StringComparer.Ordinal))
        {
            return;
        }

        isNullable.ShouldBeFalse($"Column movies.{column} must be NOT NULL.");
    }

    private static async Task AssertJoinPrimaryKeysAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        (await catalog.ListPrimaryKeyColumnsAsync("movie_actors", cancellationToken))
            .ShouldBe(["actor_id", "movie_id"], ignoreOrder: true);
        (await catalog.ListPrimaryKeyColumnsAsync("movie_directors", cancellationToken))
            .ShouldBe(["director_id", "movie_id"], ignoreOrder: true);
        (await catalog.ListPrimaryKeyColumnsAsync("movie_genres", cancellationToken))
            .ShouldBe(["genre_id", "movie_id"], ignoreOrder: true);
        (await catalog.ListPrimaryKeyColumnsAsync("movie_writers", cancellationToken))
            .ShouldBe(["writer_id", "movie_id"], ignoreOrder: true);
    }

    private static async Task AssertMovieIndexDefinitionsAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var indexes = await catalog.ListMovieIndexesAsync(cancellationToken);
        AssertTitleIndex(indexes);
        AssertReleaseYearIndex(indexes);
        AssertStatusIndex(indexes);
        AssertLibraryPathIndex(indexes);
        AssertImdbIdIndex(indexes);
    }

    private static void AssertTitleIndex(List<string> indexes) =>
        indexes.ShouldContain(def => IsTitleIndex(def));

    private static bool IsTitleIndex(string def) =>
        def.Contains("title", StringComparison.Ordinal)
        && !def.Contains("UNIQUE", StringComparison.Ordinal);

    private static void AssertReleaseYearIndex(List<string> indexes) =>
        indexes.ShouldContain(def => def.Contains("release_year", StringComparison.Ordinal));

    private static void AssertStatusIndex(List<string> indexes) =>
        indexes.ShouldContain(def => IsStatusIndex(def));

    private static bool IsStatusIndex(string def) =>
        def.Contains("status", StringComparison.Ordinal)
        && !def.Contains("imdb", StringComparison.Ordinal);

    private static void AssertLibraryPathIndex(List<string> indexes) =>
        indexes.ShouldContain(def => IsLibraryPathIndex(def));

    private static bool IsLibraryPathIndex(string def) =>
        def.Contains("library_path", StringComparison.Ordinal)
        && def.Contains("UNIQUE", StringComparison.Ordinal);

    private static void AssertImdbIdIndex(List<string> indexes) =>
        indexes.ShouldContain(def => IsImdbIdIndex(def));

    private static async Task AssertPlaybackEventsColumnsAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var columns = await catalog.ListColumnDetailsAsync("playback_events", cancellationToken);
        columns.ShouldContain(("id", "integer", false, true));
        columns.ShouldContain(("movie_id", "integer", false, false));
        columns.ShouldContain(("played_at", "timestamp with time zone", false, false));
        columns.Count.ShouldBe(3);
    }

    private static async Task AssertPlaybackEventsForeignKeyAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var foreignKeys = await catalog.ListForeignKeysAsync("playback_events", cancellationToken);
        foreignKeys.ShouldContain(("movie_id", "movies", "id", "CASCADE"));
    }

    private static async Task AssertPlaybackEventsIndexAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var indexes = await catalog.ListTableIndexesAsync("playback_events", cancellationToken);
        indexes.ShouldContain(def =>
            def.Contains("movie_id", StringComparison.Ordinal)
            && !def.Contains("UNIQUE", StringComparison.Ordinal));
    }

    private static bool IsImdbIdIndex(string def) =>
        def.Contains("imdb_id", StringComparison.Ordinal)
        && def.Contains("UNIQUE", StringComparison.Ordinal)
        && def.Contains("IS NOT NULL", StringComparison.Ordinal);

    private sealed class PostgresCatalog(LamuFlixDbContext context)
    {
        public Task<List<string>> ListPublicTablesAsync(CancellationToken cancellationToken) =>
            QueryStringsAsync(
                """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
                """,
                cancellationToken);

        public Task<List<string>> ListPostgresExtensionsAsync(CancellationToken cancellationToken) =>
            QueryStringsAsync("SELECT extname FROM pg_extension", cancellationToken);

        public Task<List<string>> ListColumnsAsync(string table, CancellationToken cancellationToken) =>
            QueryStringsAsync(
                $"""
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = '{table}'
                """,
                cancellationToken);

        public async Task<Dictionary<string, bool>> ListMoviesNullabilityAsync(CancellationToken cancellationToken)
        {
            var rows = await QueryPairsAsync(
                """
                SELECT column_name, is_nullable
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'movies'
                """,
                cancellationToken);
            return rows.ToDictionary(
                row => row.Left,
                row => row.Right == "YES",
                StringComparer.Ordinal);
        }

        public Task<List<string>> ListPrimaryKeyColumnsAsync(string table, CancellationToken cancellationToken) =>
            QueryStringsAsync(
                $"""
                SELECT kcu.column_name
                FROM information_schema.table_constraints AS tc
                JOIN information_schema.key_column_usage AS kcu
                  ON tc.constraint_name = kcu.constraint_name
                 AND tc.table_schema = kcu.table_schema
                WHERE tc.constraint_type = 'PRIMARY KEY'
                  AND tc.table_schema = 'public'
                  AND tc.table_name = '{table}'
                ORDER BY kcu.ordinal_position
                """,
                cancellationToken);

        public Task<List<string>> ListMovieIndexesAsync(CancellationToken cancellationToken) =>
            ListTableIndexesAsync("movies", cancellationToken);

        public Task<List<string>> ListTableIndexesAsync(string table, CancellationToken cancellationToken) =>
            QueryStringsAsync(
                $"""
                SELECT indexdef
                FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = '{table}'
                """,
                cancellationToken);

        public async Task<List<(string Column, string DataType, bool IsNullable, bool IsIdentity)>>
            ListColumnDetailsAsync(string table, CancellationToken cancellationToken)
        {
            var values = new List<(string Column, string DataType, bool IsNullable, bool IsIdentity)>();
            await using var command = await CreateCommandAsync(
                $"""
                SELECT column_name, data_type, is_nullable, is_identity
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = '{table}'
                """,
                cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    string.Equals(reader.GetString(2), "YES", StringComparison.Ordinal),
                    string.Equals(reader.GetString(3), "YES", StringComparison.Ordinal)));
            }

            return values;
        }

        public async Task<List<(string Column, string ReferencedTable, string ReferencedColumn, string DeleteRule)>>
            ListForeignKeysAsync(string table, CancellationToken cancellationToken)
        {
            var values = new List<(string Column, string ReferencedTable, string ReferencedColumn, string DeleteRule)>();
            await using var command = await CreateCommandAsync(
                $"""
                SELECT kcu.column_name, ccu.table_name, ccu.column_name, rc.delete_rule
                FROM information_schema.referential_constraints AS rc
                JOIN information_schema.key_column_usage AS kcu
                  ON rc.constraint_name = kcu.constraint_name
                 AND rc.constraint_schema = kcu.constraint_schema
                JOIN information_schema.constraint_column_usage AS ccu
                  ON rc.unique_constraint_name = ccu.constraint_name
                 AND rc.unique_constraint_schema = ccu.constraint_schema
                WHERE kcu.table_schema = 'public' AND kcu.table_name = '{table}'
                ORDER BY kcu.ordinal_position
                """,
                cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)));
            }

            return values;
        }

        private async Task<List<string>> QueryStringsAsync(string sql, CancellationToken cancellationToken)
        {
            var values = new List<string>();
            await using var command = await CreateCommandAsync(sql, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add(reader.GetString(0));
            }

            return values;
        }

        private async Task<List<(string Left, string Right)>> QueryPairsAsync(
            string sql,
            CancellationToken cancellationToken)
        {
            var values = new List<(string Left, string Right)>();
            await using var command = await CreateCommandAsync(sql, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add((reader.GetString(0), reader.GetString(1)));
            }

            return values;
        }

        private async Task<DbCommand> CreateCommandAsync(string sql, CancellationToken cancellationToken)
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var command = connection.CreateCommand();
            command.CommandText = sql;
            return command;
        }
    }
}
