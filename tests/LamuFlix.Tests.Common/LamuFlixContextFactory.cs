using System;
using LamuFlix.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LamuFlix.Tests.Common;

public static class LamuFlixContextFactory
{
    public static LamuFlixContext CreateContext()
    {
        var container = ContainerFixture.Postgres;
        var dbName = $"lamuflix_test_{Guid.NewGuid():N}";
        var connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = dbName
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<LamuFlixContext>()
            .UseNpgsql(connectionString)
            .Options;
        var context = new LamuFlixContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
