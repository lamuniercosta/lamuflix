using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace LamuFlix.Tests.Common;

public static class LamuFlixDbContextFactory
{
    public static LamuFlixDbContext CreateContext(PostgresFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        return CreateContext(fixture.Container);
    }

    public static LamuFlixDbContext CreateContext(PostgreSqlContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        var dbName = $"lamuflix_test_{Guid.NewGuid():N}";
        CreateDatabase(container.GetConnectionString(), dbName);
        return OpenContext(WithDatabase(container.GetConnectionString(), dbName));
    }

    public static LamuFlixDbContext OpenContext(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new LamuFlixDbContext(options);
    }

    public static async Task<LamuFlixDbContext> CreateEmptyDatabaseContextAsync(
        string adminConnectionString,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adminConnectionString);
        var dbName = $"lamuflix_test_{Guid.NewGuid():N}";
        await CreateDatabaseAsync(adminConnectionString, dbName, cancellationToken);
        return OpenContext(WithDatabase(adminConnectionString, dbName));
    }

    private static void CreateDatabase(string adminConnectionString, string dbName)
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(adminConnectionString)
            .Options;
        using var context = new LamuFlixDbContext(options);
        var connection = context.Database.GetDbConnection();
        connection.Open();
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"""CREATE DATABASE "{dbName}" """;
            command.ExecuteNonQuery();
        }
        finally
        {
            connection.Close();
        }
    }

    private static async Task CreateDatabaseAsync(
        string adminConnectionString,
        string dbName,
        CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(adminConnectionString)
            .Options;
        await using var context = new LamuFlixDbContext(options);
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{dbName}\" ";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static string WithDatabase(string connectionString, string database)
    {
        var segments = connectionString.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var found = false;
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i].StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
            {
                segments[i] = $"Database={database}";
                found = true;
            }
        }

        return found
            ? string.Join(';', segments)
            : $"{string.Join(';', segments)};Database={database}";
    }
}
