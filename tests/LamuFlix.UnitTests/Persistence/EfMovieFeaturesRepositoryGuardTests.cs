using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.UnitTests.Persistence;

public sealed class EfMovieFeaturesRepositoryGuardTests
{
    [Fact]
    public async Task UpsertAsync_NullSnapshot_ThrowsBeforeSql()
    {
        await using var context = CreateContext();
        var repository = new EfMovieFeaturesRepository(context);

        await Should.ThrowAsync<ArgumentNullException>(
            () => repository.UpsertAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpsertAsync_DefaultVector_ThrowsBeforeSql()
    {
        await using var context = CreateContext();
        var repository = new EfMovieFeaturesRepository(context);
        var snapshot = (MovieFeaturesSnapshot)RuntimeHelpers.GetUninitializedObject(
            typeof(MovieFeaturesSnapshot));

        await Should.ThrowAsync<ArgumentException>(
            () => repository.UpsertAsync(snapshot, CancellationToken.None));
    }

    private static LamuFlixDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(
                "Host=unused;Username=unused;Password=unused;Database=unused",
                o => o.UseVector())
            .Options;
        return new LamuFlixDbContext(options);
    }
}
