using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class EfMovieCatalogDetailsTests(PostgresFixture fixture)
{
    [Fact]
    public async Task GetDetailsAsync_FullMetadata_ReturnsAllPersistedMetadata()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var movie = MovieCatalogSeed.Create("File title", "details-full");
        movie.MetadataTitle = "Metadata title";
        movie.Plot = "Plot";
        movie.ReleaseYear = new ReleaseYear(2020, MovieCatalogSeed.FixedTime);
        movie.RuntimeMinutes = new Runtime(120);
        movie.ImdbRating = new ImdbRating(8.2m);
        movie.ImdbId = new ImdbId("tt1234567");
        await MovieCatalogSeed.AddAsync(context, movie, TestContext.Current.CancellationToken);
        var id = movie.Id!;
        await using var detailsContext = LamuFlixDbContextFactory.OpenContext(context.Database.GetConnectionString()!);

        // act
        var details = await new EfMovieCatalog(detailsContext).GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Id.ShouldBe(id);
        details.Title.ShouldBe("File title");
        details.Path.ShouldBe(movie.LibraryPath);
        details.Format.ShouldBe(movie.Format);
        details.Metadata.ShouldBe(new MovieMetadata("Metadata title", "Plot", movie.ReleaseYear, movie.RuntimeMinutes, movie.ImdbRating, movie.ImdbId));
        detailsContext.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDetailsAsync_NoMetadata_ReturnsNullMetadata()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var movie = await MovieCatalogSeed.AddAsync(context, MovieCatalogSeed.Create("No metadata", "details-none"), TestContext.Current.CancellationToken);

        // act
        var details = await new EfMovieCatalog(context).GetDetailsAsync(movie.Id!, TestContext.Current.CancellationToken);

        // assert
        details!.Metadata.ShouldBeNull();
    }

    [Fact]
    public async Task GetDetailsAsync_TitleOnlyMetadata_ReturnsTitleOnlyMetadata()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var movie = MovieCatalogSeed.Create("File", "details-title-only");
        movie.MetadataTitle = "Metadata";
        await MovieCatalogSeed.AddAsync(context, movie, TestContext.Current.CancellationToken);

        // act
        var details = await new EfMovieCatalog(context).GetDetailsAsync(movie.Id!, TestContext.Current.CancellationToken);

        // assert
        details!.Metadata.ShouldBe(new MovieMetadata("Metadata"));
    }

    [Fact]
    public async Task GetDetailsAsync_PartialMetadata_FallsBackToFileTitle()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var movie = MovieCatalogSeed.Create("File fallback", "details-partial");
        movie.Plot = "Plot only";
        await MovieCatalogSeed.AddAsync(context, movie, TestContext.Current.CancellationToken);

        // act
        var details = await new EfMovieCatalog(context).GetDetailsAsync(movie.Id!, TestContext.Current.CancellationToken);

        // assert
        details!.Metadata.ShouldBe(new MovieMetadata("File fallback", "Plot only"));
    }

    [Fact]
    public async Task GetDetailsAsync_MissingId_ReturnsNull()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);

        // act
        var details = await new EfMovieCatalog(context).GetDetailsAsync(new MovieId(1), TestContext.Current.CancellationToken);

        // assert
        details.ShouldBeNull();
    }

    [Fact]
    public async Task GetDetailsAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // act
        var act = () => new EfMovieCatalog(context).GetDetailsAsync(new MovieId(1), cancellation.Token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }
}