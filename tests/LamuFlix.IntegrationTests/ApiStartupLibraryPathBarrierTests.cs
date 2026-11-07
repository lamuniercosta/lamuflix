using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class ApiStartupLibraryPathBarrierTests(PostgresFixture fixture)
{
    private const string MigrationsKey =
        $"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ApplyMigrationsOnStartup)}";
    private const string RootKey = $"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}";
    private const string ActiveRoot = "C:/library";

    [Fact]
    public async Task Startup_WithMigrationsEnabled_ActivatesTheLibraryPathKeyBeforeServingHttp()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var empty = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var connectionString = fixture.FullConnectionString(empty);
        await empty.Database.MigrateAsync(cancellationToken);
        await InsertLegacyRowAsync(empty, "Barrier", "C:/library/Barrier/barrier.mkv", cancellationToken);

        await using var factory = new ApiHostFactory(
            retainSweeper: false,
            additionalSettings: Settings(connectionString, applyMigrations: true),
            disableEnrichmentConsumer: true);

        // act
        using var client = factory.CreateClient();
        var liveness = await client.GetAsync("/health/live", cancellationToken);

        // assert
        liveness.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var observed = LamuFlixDbContextFactory.OpenContext(connectionString);
        var row = await ReadScalarAsync(
            observed,
            "SELECT (library_path || '|' || library_path_key) FROM movies",
            cancellationToken);
        row.ShouldBe("Barrier/barrier.mkv|BARRIER/BARRIER.MKV");
        var collation = await ReadScalarAsync(
            observed,
            """
            SELECT COALESCE((SELECT coll.collname FROM pg_collation AS coll
                             WHERE coll.oid = attribute.attcollation), '')
            FROM pg_attribute AS attribute
            JOIN pg_class AS relation ON relation.oid = attribute.attrelid
            JOIN pg_namespace AS space ON space.oid = relation.relnamespace
            WHERE space.nspname = 'public' AND relation.relname = 'movies'
              AND attribute.attname = 'library_path_key' AND attribute.attnum > 0
              AND NOT attribute.attisdropped
            """,
            cancellationToken);
        collation.ShouldBe("C");
    }

    [Fact]
    public async Task Startup_WithMigrationsEnabledOnCollidingRows_FailsClosedAndServesNoHttp()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var empty = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var connectionString = fixture.FullConnectionString(empty);
        await empty.Database.MigrateAsync(cancellationToken);
        await InsertLegacyRowAsync(empty, "Upper", "C:/library/Clash/clash.mkv", cancellationToken);
        await InsertLegacyRowAsync(empty, "Lower", "C:/library/clash/clash.mkv", cancellationToken);

        await using var factory = new ApiHostFactory(
            retainSweeper: false,
            additionalSettings: Settings(connectionString, applyMigrations: true),
            disableEnrichmentConsumer: true);

        // act
        var failure = Should.Throw<Exception>(() => factory.CreateClient());

        // assert
        failure.ToString().ShouldContain("preflight failed closed");
        failure.ToString().ShouldContain("Clash");
        await using var observed = LamuFlixDbContextFactory.OpenContext(connectionString);
        (await observed.Database
            .SqlQueryRaw<string>("SELECT COALESCE(library_path_key, '<null>') AS \"Value\" FROM movies")
            .ToListAsync(cancellationToken))
            .ShouldAllBe(value => value == "<null>");
    }

    [Fact]
    public async Task Startup_WithMigrationsDisabledOnUnactivatedSchema_FailsClosedAndServesNoHttp()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var empty = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var connectionString = fixture.FullConnectionString(empty);
        await empty.Database.MigrateAsync(cancellationToken);

        await using var factory = new ApiHostFactory(
            retainSweeper: false,
            additionalSettings: Settings(connectionString, applyMigrations: false),
            disableEnrichmentConsumer: true);

        // act
        var failure = Should.Throw<Exception>(() => factory.CreateClient());

        // assert
        failure.ToString().ShouldContain("Schema not ready");
    }

    [Fact]
    public async Task Startup_WithMigrationsDisabledOnActivatedSchema_ServesHttp()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var empty = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var connectionString = fixture.FullConnectionString(empty);
        await empty.Database.MigrateAsync(cancellationToken);
        await PostgresFixture.ActivateLibraryPathKeyAsync(empty, cancellationToken);

        await using var factory = new ApiHostFactory(
            retainSweeper: false,
            additionalSettings: Settings(connectionString, applyMigrations: false),
            disableEnrichmentConsumer: true);

        // act
        using var client = factory.CreateClient();
        var liveness = await client.GetAsync("/health/live", cancellationToken);

        // assert
        liveness.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<string> ReadScalarAsync(
        LamuFlixDbContext context,
        string sql,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture)
            ?? string.Empty;
    }

    private static IEnumerable<KeyValuePair<string, string?>> Settings(string connectionString, bool applyMigrations) =>
    [
        new("ConnectionStrings:DefaultConnection", connectionString),
        new(MigrationsKey, applyMigrations ? "true" : "false"),
        new(RootKey, ActiveRoot),
    ];

    private static Task InsertLegacyRowAsync(
        LamuFlixDbContext context,
        string title,
        string libraryPath,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlRawAsync(
            "INSERT INTO movies (title, library_path, format, status, enrichment_attempts, is_in_watchlist) VALUES ({0}, {1}, 'mkv', 0, 0, false)",
            [title, libraryPath],
            cancellationToken);
}
