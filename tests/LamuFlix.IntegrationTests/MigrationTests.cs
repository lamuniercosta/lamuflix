using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class MigrationTests(PostgresFixture fixture)
{
    private static readonly string[] ApplicationTables =
    [
        "movies",
        "actors",
        "directors",
        "genres",
        "movie_actors",
        "movie_directors",
        "movie_genres"
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
        "last_attempt_at"
    ];

    private static readonly Regex SnakeCase = new(
        "^[a-z][a-z0-9]*(_[a-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [Fact]
    public async Task MigrateAsync_EmptyDatabase_AppliesInitialAndHasNoPendingModelChanges()
    {
        await using var context = fixture.CreateContext();

        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var expected = context.Database.GetMigrations().ShouldHaveSingleItem();
        expected.ShouldEndWith("_Initial");
        var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        applied.ShouldBe([expected]);
        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateAsync_EmptyDatabase_MatchesPostgresCatalogContract()
    {
        await using var context = fixture.CreateContext();
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
            QueryStringsAsync(
                """
                SELECT indexdef
                FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = 'movies'
                """,
                cancellationToken);

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
