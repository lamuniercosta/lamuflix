using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class LamuFlixDesignTimeDbContextFactory : IDesignTimeDbContextFactory<LamuFlixDbContext>
{
    public const string ConnectionStringVariable = "LAMUFLIX_DESIGN_TIME_CONNECTION_STRING";

    public LamuFlixDbContext CreateDbContext(string[] args)
    {
        _ = args;
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{ConnectionStringVariable} is missing or blank. Set it to a PostgreSQL connection string.");
        }

        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new LamuFlixDbContext(options);
    }
}
