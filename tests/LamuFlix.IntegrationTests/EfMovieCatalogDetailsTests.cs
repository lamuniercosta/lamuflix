using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class EfMovieCatalogDetailsTests(PostgresFixture fixture) : IAsyncLifetime, IClassFixture<PostgresFixture>
{
    private static readonly string[] AssociationTables =
    [
        "movie_actors",
        "movie_directors",
        "movie_genres",
        "movie_writers",
        "movie_keywords",
        "movie_tmdb_cast",
    ];

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetDetailsAsync_FullMetadata_ReturnsAllPersistedMetadata()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var movie = MovieCatalogSeed.Create("File title", "details-full");
        movie.MetadataTitle = "Metadata title";
        movie.Plot = "Plot";
        movie.ReleaseYear = new ReleaseYear(2020, MovieCatalogSeed.FixedTime);
        movie.RuntimeMinutes = new Runtime(120);
        movie.ImdbRating = new ImdbRating(8.2m);
        movie.ImdbId = new ImdbId("tt1234567");
        await MovieCatalogSeed.AddAsync(context, movie, TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var connectionString = fixture.ConnectionString;
        connectionString.ShouldNotBeNull();
        await using var detailsContext = LamuFlixDbContextFactory.OpenContext(connectionString);

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
    public async Task GetDetailsAsync_CompleteSnapshot_ReturnsGenresAndWritersWithTheRestOfTheSnapshot()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var movie = MovieCatalogSeed.Create("File title", "details-complete");
        movie.MetadataTitle = "Metadata title";
        movie.Plot = "Full synopsis";
        movie.MetaScore = 88;
        movie.ReleaseYear = new ReleaseYear(2020, MovieCatalogSeed.FixedTime);
        movie.RuntimeMinutes = new Runtime(120);
        movie.ImdbRating = new ImdbRating(8.2m);
        movie.ImdbId = new ImdbId("tt1234567");
        movie.Directors.Add(new DirectorRecord { Name = "Dana Director" });
        movie.Actors.Add(new ActorRecord { Name = "Ada Actor" });
        movie.Genres.Add(new GenreRecord { Name = "Drama" });
        movie.Genres.Add(new GenreRecord { Name = "War" });
        movie.Writers.Add(new WriterRecord { Name = "Wanda Writer" });
        await MovieCatalogSeed.AddAsync(context, movie, TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var connectionString = fixture.ConnectionString;
        connectionString.ShouldNotBeNull();
        await using var detailsContext = LamuFlixDbContextFactory.OpenContext(connectionString);

        // act
        var details = await new EfMovieCatalog(detailsContext).GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        var metadata = details.Metadata.ShouldNotBeNull();
        metadata.Synopsis.ShouldBe("Full synopsis");
        metadata.MetaScore.ShouldBe((short)88);
        metadata.Directors.ShouldBe(["Dana Director"]);
        metadata.Actors.ShouldBe(["Ada Actor"]);
        metadata.Genres.ShouldBe(["Drama", "War"], ignoreOrder: true);
        metadata.Writers.ShouldBe(["Wanda Writer"]);
        detailsContext.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDetailsAsync_NoMetadata_ReturnsNullMetadata()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var movie = await MovieCatalogSeed.AddAsync(context, MovieCatalogSeed.Create("No metadata", "details-none"), TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();

        // act
        var details = await new EfMovieCatalog(context).GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldBeNull();
    }

    [Fact]
    public async Task GetDetailsAsync_TitleOnlyMetadata_ReturnsTitleOnlyMetadata()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var movie = MovieCatalogSeed.Create("File", "details-title-only");
        movie.MetadataTitle = "Metadata";
        await MovieCatalogSeed.AddAsync(context, movie, TestContext.Current.CancellationToken);

        // act
        var id = movie.Id;
        id.ShouldNotBeNull();
        var details = await new EfMovieCatalog(context).GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldBe(new MovieMetadata("Metadata"));
    }

    [Fact]
    public async Task GetDetailsAsync_PartialMetadata_FallsBackToFileTitle()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var movie = MovieCatalogSeed.Create("File fallback", "details-partial");
        movie.Plot = "Plot only";
        await MovieCatalogSeed.AddAsync(context, movie, TestContext.Current.CancellationToken);

        // act
        var id = movie.Id;
        id.ShouldNotBeNull();
        var details = await new EfMovieCatalog(context).GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldBe(new MovieMetadata("File fallback", "Plot only"));
    }

    [Fact]
    public async Task GetDetailsAsync_MissingId_ReturnsNull()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();

        // act
        var details = await new EfMovieCatalog(context).GetDetailsAsync(new MovieId(1), TestContext.Current.CancellationToken);

        // assert
        details.ShouldBeNull();
    }

    [Fact]
    public async Task GetDetailsAsync_DenseAssociations_LoadsEveryCollectionWithoutFanningThemIntoOneJoin()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var movie = MovieCatalogSeed.Create("Dense", "details-dense");
        movie.MetadataTitle = "Dense";
        movie.CollectionId = 900;
        context.Collections.Add(new CollectionRecord { Id = 900, Name = "Franchise 900" });
        movie.Directors.Add(new DirectorRecord { Name = "Director One" });
        movie.Directors.Add(new DirectorRecord { Name = "Director Two" });
        movie.Actors.Add(new ActorRecord { Name = "Actor One" });
        movie.Actors.Add(new ActorRecord { Name = "Actor Two" });
        movie.Actors.Add(new ActorRecord { Name = "Actor Three" });
        movie.Genres.Add(new GenreRecord { Name = "Drama" });
        movie.Genres.Add(new GenreRecord { Name = "War" });
        movie.Genres.Add(new GenreRecord { Name = "History" });
        movie.Writers.Add(new WriterRecord { Name = "Writer One" });
        movie.Writers.Add(new WriterRecord { Name = "Writer Two" });
        movie.Keywords.Add(new KeywordRecord { Id = 101, Name = "Heist" });
        movie.Keywords.Add(new KeywordRecord { Id = 102, Name = "Noir" });
        movie.Keywords.Add(new KeywordRecord { Id = 103, Name = "Caper" });
        movie.Keywords.Add(new KeywordRecord { Id = 104, Name = "Betrayal" });
        movie.FullCast.Add(new ActorRecord { Name = "Actor One" });
        movie.FullCast.Add(new ActorRecord { Name = "Actor Two" });
        movie.FullCast.Add(new ActorRecord { Name = "Actor Three" });
        movie.FullCast.Add(new ActorRecord { Name = "Actor Four" });
        movie.FullCast.Add(new ActorRecord { Name = "Actor Five" });
        await MovieCatalogSeed.AddAsync(context, movie, TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var interceptor = new ReaderCommandCaptureInterceptor();
        await using var detailsContext = OpenInstrumentedContext(interceptor);

        // act
        var details = await new EfMovieCatalog(detailsContext).GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        var metadata = details.Metadata.ShouldNotBeNull();
        metadata.CollectionName.ShouldBe("Franchise 900");
        metadata.Directors.ShouldBe(["Director One", "Director Two"], ignoreOrder: true);
        metadata.Actors.ShouldBe(["Actor One", "Actor Two", "Actor Three"], ignoreOrder: true);
        metadata.Genres.ShouldBe(["Drama", "War", "History"], ignoreOrder: true);
        metadata.Writers.ShouldBe(["Writer One", "Writer Two"], ignoreOrder: true);
        var signals = metadata.Signals.ShouldNotBeNull();
        signals.Keywords.Select(keyword => keyword.Name).ShouldBe(["Caper", "Betrayal", "Heist", "Noir"], ignoreOrder: true);
        signals.FullCast.Order(StringComparer.Ordinal).ShouldBe(
            ["Actor Five", "Actor Four", "Actor One", "Actor Three", "Actor Two"]);
        detailsContext.ChangeTracker.Entries().ShouldBeEmpty();
        interceptor.Commands.ShouldNotBeEmpty();
        interceptor.Commands.Max(AssociationJoins).ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetDetailsAsync_MissingId_ReturnsNullWithoutFanningCollections()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var movie = MovieCatalogSeed.Create("Present", "details-present");
        movie.Keywords.Add(new KeywordRecord { Id = 201, Name = "Heist" });
        movie.FullCast.Add(new ActorRecord { Name = "Guest Actor" });
        await MovieCatalogSeed.AddAsync(context, movie, TestContext.Current.CancellationToken);
        var interceptor = new ReaderCommandCaptureInterceptor();
        await using var missingContext = OpenInstrumentedContext(interceptor);

        // act
        var details = await new EfMovieCatalog(missingContext)
            .GetDetailsAsync(new MovieId(9999), TestContext.Current.CancellationToken);

        // assert
        details.ShouldBeNull();
        interceptor.Commands.ShouldNotBeEmpty();
        interceptor.Commands.Max(AssociationJoins).ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetDetailsAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var catalog = new EfMovieCatalog(context);
        var token = cancellation.Token;

        // act
        var act = () => catalog.GetDetailsAsync(new MovieId(1), token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    private LamuFlixDbContext OpenInstrumentedContext(DbCommandInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(fixture.ConnectionString, o => o.UseVector())
            .AddInterceptors(interceptor)
            .Options;
        return new LamuFlixDbContext(options);
    }

    private static int AssociationJoins(string command)
    {
        var counted = 0;
        foreach (var table in AssociationTables)
        {
            if (command.Contains(table, StringComparison.Ordinal))
            {
                counted++;
            }
        }

        return counted;
    }

    private sealed class ReaderCommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

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