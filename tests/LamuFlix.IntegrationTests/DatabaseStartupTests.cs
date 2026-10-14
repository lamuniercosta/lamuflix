using System.Collections.Generic;
using System.Data;
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
public sealed class DatabaseStartupTests(PostgresFixture fixture)
{
    private const string MigrationsHistoryTable = "__EFMigrationsHistory";
    private const string DatabaseSettingKey =
        $"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ApplyMigrationsOnStartup)}";

    [Fact]
    public async Task AbsentDatabaseSetting_DoesNotApplyMigrations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var empty = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var connectionString = empty.Database.GetConnectionString();
        connectionString.ShouldNotBeNullOrWhiteSpace();

        var before = await ListPublicBaseTablesAsync(empty, cancellationToken);
        before.ShouldBeEmpty();

        await using var factory = new ApiHostFactory(
            retainSweeper: false,
            additionalSettings: ConnectionOverride(connectionString),
            disableEnrichmentConsumer: true);
        using var client = factory.CreateClient();
        var liveness = await client.GetAsync("/health/live", cancellationToken);

        liveness.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var observed = LamuFlixDbContextFactory.OpenContext(connectionString);
        var after = await ListPublicBaseTablesAsync(observed, cancellationToken);
        after.ShouldBeEmpty();
        after.ShouldNotContain(MigrationsHistoryTable);
    }

    [Fact]
    public async Task FalseDatabaseSetting_DoesNotApplyMigrations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var empty = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var connectionString = empty.Database.GetConnectionString();
        connectionString.ShouldNotBeNullOrWhiteSpace();

        var before = await ListPublicBaseTablesAsync(empty, cancellationToken);
        before.ShouldBeEmpty();

        var settings = new List<KeyValuePair<string, string?>>(ConnectionOverride(connectionString))
        {
            new(DatabaseSettingKey, "false"),
        };
        await using var factory = new ApiHostFactory(
            retainSweeper: false,
            additionalSettings: settings,
            disableEnrichmentConsumer: true);
        using var client = factory.CreateClient();
        var liveness = await client.GetAsync("/health/live", cancellationToken);

        liveness.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var observed = LamuFlixDbContextFactory.OpenContext(connectionString);
        var after = await ListPublicBaseTablesAsync(observed, cancellationToken);
        after.ShouldBeEmpty();
        after.ShouldNotContain(MigrationsHistoryTable);
    }

    [Fact]
    public async Task TrueDatabaseSetting_AppliesMigrations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var empty = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var connectionString = empty.Database.GetConnectionString();
        connectionString.ShouldNotBeNullOrWhiteSpace();

        var before = await ListPublicBaseTablesAsync(empty, cancellationToken);
        before.ShouldBeEmpty();

        var settings = new List<KeyValuePair<string, string?>>(ConnectionOverride(connectionString))
        {
            new(DatabaseSettingKey, "true"),
        };
        await using var factory = new ApiHostFactory(
            retainSweeper: false,
            additionalSettings: settings,
            disableEnrichmentConsumer: true);
        using var client = factory.CreateClient();
        var liveness = await client.GetAsync("/health/live", cancellationToken);

        liveness.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var observed = LamuFlixDbContextFactory.OpenContext(connectionString);
        var after = await ListPublicBaseTablesAsync(observed, cancellationToken);
        after.ShouldContain(MigrationsHistoryTable);
        after.ShouldContain("movies");
    }

    private static IEnumerable<KeyValuePair<string, string?>> ConnectionOverride(string connectionString) =>
    [
        new("ConnectionStrings:DefaultConnection", connectionString),
    ];

    private static async Task<List<string>> ListPublicBaseTablesAsync(
        LamuFlixDbContext context,
        CancellationToken cancellationToken)
    {
        var tables = new List<string>();
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            ORDER BY table_name
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}
