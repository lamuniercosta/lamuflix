using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Testcontainers.PostgreSql;
using Xunit;

namespace LamuFlix.Tests.Common;

public sealed class PostgresFixture : IAsyncLifetime
{
    private const string MigrationsHistoryTable = "__EFMigrationsHistory";
    private const string DefaultSchema = "public";
    private const string LockTimeoutStatement = "SET LOCAL lock_timeout = '10s'";

    private PostgreSqlContainer? _container;
    private string? _truncateStatement;

    public PostgreSqlContainer Container =>
        _container ?? throw new InvalidOperationException(
            "PostgresFixture has not been initialized. InitializeAsync must run first.");

    public string ConnectionString => Container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync(cancellationToken);
            await MigrateAsync(cancellationToken);
            _truncateStatement = BuildTruncate(CreateMigratedContext());
        }
        catch
        {
            await DiscardAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not { } started)
        {
            return;
        }

        await started.DisposeAsync();
        _container = null;
    }

    public LamuFlixDbContext CreateMigratedContext() =>
        LamuFlixDbContextFactory.OpenContext(ConnectionString);

    public async Task<LamuFlixDbContext> CreateEmptyDatabaseContextAsync(CancellationToken cancellationToken) =>
        await LamuFlixDbContextFactory.CreateEmptyDatabaseContextAsync(ConnectionString, cancellationToken);

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        var statement = _truncateStatement ?? throw new InvalidOperationException(
            "PostgresFixture has not been initialized. InitializeAsync must run first.");
        await using var context = CreateMigratedContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync(LockTimeoutStatement, cancellationToken);
        await context.Database.ExecuteSqlRawAsync(statement, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateMigratedContext();
        await context.Database.MigrateAsync(cancellationToken);
    }

    private static string BuildTruncate(LamuFlixDbContext context)
    {
        var delimits = context.GetService<ISqlGenerationHelper>();
        var targets = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var table = entityType.GetTableName();
            if (table is null || string.Equals(table, MigrationsHistoryTable, StringComparison.Ordinal))
            {
                continue;
            }

            targets.Add(delimits.DelimitIdentifier(table, entityType.GetSchema() ?? DefaultSchema));
        }

        return $"TRUNCATE TABLE {string.Join(", ", targets)} RESTART IDENTITY CASCADE";
    }

    private async ValueTask DiscardAsync()
    {
        if (_container is not { } started)
        {
            return;
        }

        _container = null;
        try
        {
            await started.DisposeAsync();
        }
        catch (Exception)
        {
            // Deliberately empty: a cleanup failure must not replace the initialization failure.
        }
    }
}