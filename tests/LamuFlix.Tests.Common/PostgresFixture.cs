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
    private const string DefaultSchema = "public";
    private const string LockTimeoutStatement = "SET LOCAL lock_timeout = '10s'";
    private const int MaxInitAttempts = 4;

    private static readonly TimeSpan[] InitRetryDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
    ];

    private PostgreSqlContainer? _container;

    public PostgreSqlContainer Container =>
        _container ?? throw new InvalidOperationException(
            "PostgresFixture has not been initialized. InitializeAsync must run first.");

    public string ConnectionString => Container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        Exception? lastFailure = null;
        for (var attempt = 1; attempt <= MaxInitAttempts; attempt++)
        {
            try
            {
                await DiscardAsync();
                _container = new PostgreSqlBuilder(
                    "pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d").Build();
                await _container.StartAsync(cancellationToken);
                await MigrateAsync(cancellationToken);
                return;
            }
            catch (Exception failure) when (attempt < MaxInitAttempts && !cancellationToken.IsCancellationRequested)
            {
                lastFailure = failure;
                await DiscardAsync();
                await Task.Delay(InitRetryDelays[attempt - 1], cancellationToken);
            }
            catch
            {
                await DiscardAsync();
                throw;
            }
        }

        throw lastFailure ?? new InvalidOperationException("PostgresFixture initialization failed.");
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
        await using var context = CreateMigratedContext();
        var statement = await BuildTruncateAsync(context, cancellationToken);
        if (statement is null)
        {
            return;
        }

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

    private static async Task<string?> BuildTruncateAsync(
        LamuFlixDbContext context,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'
            ORDER BY table_name
            """;
        var tables = await context.Database
            .SqlQueryRaw<string>(sql)
            .ToListAsync(cancellationToken);
        if (tables.Count == 0)
        {
            return null;
        }

        var delimits = context.GetService<ISqlGenerationHelper>();
        var targets = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            targets.Add(delimits.DelimitIdentifier(table, DefaultSchema));
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
