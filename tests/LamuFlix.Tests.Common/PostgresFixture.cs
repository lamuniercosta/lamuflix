using System;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace LamuFlix.Tests.Common;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public PostgreSqlContainer Container =>
        _container ?? throw new InvalidOperationException(
            "PostgresFixture has not been initialized. InitializeAsync must run first.");

    public async ValueTask InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16.4").Build();
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public LamuFlixDbContext CreateContext() => LamuFlixDbContextFactory.CreateContext(this);
}
