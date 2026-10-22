using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.UnitTests.Persistence;

public sealed class EfMovieFeaturesRepositoryGuardTests
{
    [Fact]
    public async Task UpsertAsync_NullSnapshot_ThrowsBeforeSql()
    {
        await using var context = CreateContext();
        var repository = new EfMovieFeaturesRepository(context);
        var upsertAsync = typeof(EfMovieFeaturesRepository).GetMethod(nameof(EfMovieFeaturesRepository.UpsertAsync))
            ?? throw new InvalidOperationException("EfMovieFeaturesRepository UpsertAsync method was not found.");

        var invocation = (Task)(upsertAsync.Invoke(repository, [null, CancellationToken.None])
            ?? throw new InvalidOperationException("EfMovieFeaturesRepository UpsertAsync invocation returned null."));
        var exception = await Should.ThrowAsync<ArgumentNullException>(() => invocation);

        exception.ParamName.ShouldBe("features");
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
