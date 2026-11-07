using System;
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

    [Theory]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData("true", true)]
    public async Task ApplyMigrationsOnStartupSetting_DeterminesStartupMigrationOutcome(
        string? configuredValue,
        bool expectMigrationsApplied)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var empty = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var database = empty.Database.GetDbConnection().Database;
        database.ShouldNotBeNullOrWhiteSpace();
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = database };
        var connectionString = builder.ConnectionString;
        connectionString.ShouldNotBeNullOrWhiteSpace();

        var before = await ListPublicBaseTablesAsync(empty, cancellationToken);
        before.ShouldBeEmpty();

        var settings = new List<KeyValuePair<string, string?>>(ConnectionOverride(connectionString));
        if (configuredValue is not null)
        {
            settings.Add(new(DatabaseSettingKey, configuredValue));
        }

        await using var factory = new ApiHostFactory(
            retainSweeper: false,
            additionalSettings: settings,
            disableEnrichmentConsumer: true);

        if (expectMigrationsApplied)
        {
            using var appliedClient = factory.CreateClient();
            var liveness = await appliedClient.GetAsync("/health/live", cancellationToken);
            liveness.StatusCode.ShouldBe(HttpStatusCode.OK);

            await using var observed = LamuFlixDbContextFactory.OpenContext(connectionString);
            var after = await ListPublicBaseTablesAsync(observed, cancellationToken);
            after.ShouldContain(MigrationsHistoryTable);
            after.ShouldContain("movies");
            return;
        }

        var failure = Should.Throw<Exception>(() => factory.CreateClient());
        failure.ToString().ShouldContain("Schema not ready");

        await using var untouched = LamuFlixDbContextFactory.OpenContext(connectionString);
        var tables = await ListPublicBaseTablesAsync(untouched, cancellationToken);
        tables.ShouldBeEmpty();
        tables.ShouldNotContain(MigrationsHistoryTable);
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
