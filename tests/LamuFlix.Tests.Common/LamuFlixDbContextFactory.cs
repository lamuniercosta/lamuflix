using System;
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
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(WithDatabase(container.GetConnectionString(), dbName))
            .Options;
        return new LamuFlixDbContext(options);
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
