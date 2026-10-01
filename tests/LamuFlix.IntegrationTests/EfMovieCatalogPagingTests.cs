using System.Data.Common;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shouldly;
using Xunit;
using SortDirection = LamuFlix.Core.Library.SortDirection;

namespace LamuFlix.IntegrationTests;

public sealed class EfMovieCatalogPagingTests(PostgresFixture fixture)
{
    [Fact]
    public async Task BrowseAsync_PagesAndPastEnd_ReturnsCorrectCountWithoutGaps()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        context.Movies.AddRange(
            MovieCatalogSeed.Create("Tie", "page-1"),
            MovieCatalogSeed.Create("Tie", "page-2"),
            MovieCatalogSeed.Create("Tie", "page-3"),
            MovieCatalogSeed.Create("Tie", "page-4"),
            MovieCatalogSeed.Create("Tie", "page-5"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await using var browseContext = LamuFlixDbContextFactory.OpenContext(context.Database.GetConnectionString()!);
        var catalog = new EfMovieCatalog(browseContext);

        // act
        var first = await catalog.BrowseAsync(Query(1, 2), TestContext.Current.CancellationToken);
        var middle = await catalog.BrowseAsync(Query(2, 2), TestContext.Current.CancellationToken);
        var last = await catalog.BrowseAsync(Query(3, 2), TestContext.Current.CancellationToken);
        var pastEnd = await catalog.BrowseAsync(Query(4, 2), TestContext.Current.CancellationToken);

        // assert
        first.TotalCount.ShouldBe(5);
        middle.TotalCount.ShouldBe(5);
        last.TotalCount.ShouldBe(5);
        pastEnd.TotalCount.ShouldBe(5);
        first.Items.Length.ShouldBe(2);
        middle.Items.Length.ShouldBe(2);
        last.Items.Length.ShouldBe(1);
        pastEnd.Items.ShouldBeEmpty();
        first.Items.Select(item => item.Id).Intersect(middle.Items.Select(item => item.Id)).ShouldBeEmpty();
        first.Items.Concat(middle.Items).Concat(last.Items).Select(item => item.Id).Distinct().Count().ShouldBe(5);
        browseContext.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task BrowseAsync_PageQuery_SelectsOnlySummaryColumns()
    {
        // arrange
        var interceptor = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(fixture.Container.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;
        await using var context = new LamuFlixDbContext(options);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var movie = MovieCatalogSeed.Create("Projection", "projection");
        movie.Genres.Add(new GenreRecord { Name = "Drama" });
        context.Movies.Add(movie);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await context.DisposeAsync();
        await using var browseContext = new LamuFlixDbContext(options);
        await browseContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        interceptor.Commands.Clear();
        var catalog = new EfMovieCatalog(browseContext);

        // act
        await catalog.BrowseAsync(Query(1, 10), TestContext.Current.CancellationToken);

        // assert
        var select = interceptor.Commands.Single(command =>
            command.Contains("SELECT", System.StringComparison.OrdinalIgnoreCase)
            && !command.Contains("COUNT(", System.StringComparison.OrdinalIgnoreCase));
        select.ToLowerInvariant().Split("from", System.StringSplitOptions.None)[0].ShouldContain("title");
        select.ToLowerInvariant().Split("from", System.StringSplitOptions.None)[0].ShouldContain("id");
        select.ShouldNotContain("JOIN", Case.Sensitive);
        browseContext.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task BrowseAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var catalog = new EfMovieCatalog(context);

        // act
        var act = () => catalog.BrowseAsync(Query(1, 1), cancellation.Token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    private static MovieQuery Query(int number, int size) => new()
    {
        Sort = MovieSort.Title,
        Direction = SortDirection.Ascending,
        Page = new Page(number, size)
    };

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        internal List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}