using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfMovieRepositoryRescanTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task HandleAsync_ReplacedExtension_UpdatesExactlyOneRowAndPreservesEveryOtherColumn()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var seeded = await SeedAsync(db, ct);
        var scanner = ScannerReturning(seeded.Baseline, "mkv");
        var handler = new RescanMovieCommandHandler(Repository(db), scanner);

        // act
        var result = await handler.HandleAsync(new RescanMovieCommand(seeded.Id), ct);

        // assert
        result.Changed.ShouldBeTrue();
        result.OldFormat.ShouldBe(new MediaFormat("avi"));
        result.NewFormat.ShouldBe(new MediaFormat("mkv"));

        await using var verify = OpenSibling(db);
        var rows = await verify.Movies.AsNoTracking()
            .Where(movie => movie.Id == seeded.Id)
            .ToArrayAsync(ct);
        var row = rows.ShouldHaveSingleItem();
        row.LibraryPath.ShouldBe(new LibraryPath("C:/library/Heat"));
        row.Format.ShouldBe(new MediaFormat("mkv"));
        row.Title.ShouldBe("Heat");
        row.ParsedReleaseYear.ShouldBe(new ReleaseYear(1995, Now));
        row.IsInWatchlist.ShouldBeTrue();
        row.IsWatched.ShouldBeTrue();
        row.Review.ShouldBe(Review.Disliked);
        row.Status.ShouldBe(EnrichmentStatus.Enriched);
        row.EnrichedAt.ShouldBe(Now);
        row.EnrichmentAttempts.ShouldBe(3);
        row.MetadataTitle.ShouldBe("Heat");
        row.Plot.ShouldBe("A crew of thieves.");
        row.ReleaseYear.ShouldBe(new ReleaseYear(1995, Now));
        row.RuntimeMinutes.ShouldBe(new Runtime(170));
        row.ImdbRating.ShouldBe(new ImdbRating(8.3m));
        row.ImdbId.ShouldBe(new ImdbId("tt0113277"));
        row.PosterUrl.ShouldBe("https://example.invalid/heat.jpg");
        (await verify.Movies.CountAsync(movie => movie.LibraryPath == new LibraryPath("C:/library/Heat"), ct))
            .ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_EnrichmentWriteBetweenLoadAndSave_SurvivesTheFormatSave()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var seeded = await SeedAsync(db, ct);
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.ResolvePrimaryVideo(seeded.Baseline)
            .Returns(_ =>
            {
                MarkNotFoundBetweenLoadAndSave(seeded.Id);
                return new PrimaryVideo(
                    new LibraryPath("C:/library/Heat/Heat.mkv"),
                    new MediaFormat("mkv"));
            });
        var handler = new RescanMovieCommandHandler(Repository(db), scanner);

        // act
        var result = await handler.HandleAsync(new RescanMovieCommand(seeded.Id), ct);

        // assert
        result.Changed.ShouldBeTrue();
        await using var verify = OpenSibling(db);
        var row = await verify.Movies.AsNoTracking().SingleAsync(movie => movie.Id == seeded.Id, ct);
        row.Format.ShouldBe(new MediaFormat("mkv"));
        row.Status.ShouldBe(EnrichmentStatus.NotFound);
        row.EnrichmentAttempts.ShouldBe(9);
        row.MetadataTitle.ShouldBe("Heat");
    }

    [Fact]
    public async Task HandleAsync_UnchangedFormat_IssuesNoUpdate()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var seeded = await SeedAsync(db, ct);
        var capture = new ClaimCommandCaptureInterceptor();
        await using var capturing = new LamuFlixDbContext(new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(fixture.ConnectionString, options => options.UseVector())
            .AddInterceptors(capture)
            .Options);
        var handler = new RescanMovieCommandHandler(
            Repository(capturing),
            ScannerReturning(seeded.Baseline, "avi"));

        // act
        var result = await handler.HandleAsync(new RescanMovieCommand(seeded.Id), ct);

        // assert
        result.Changed.ShouldBeFalse();
        capture.Commands.ShouldNotContain(command =>
            command.Contains("UPDATE", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<(MovieId Id, LibraryPath Baseline)> SeedAsync(LamuFlixDbContext db, CancellationToken ct)
    {
        var record = MovieCatalogSeed.Create("Heat", "Heat");
        record.LibraryPath = new LibraryPath("C:/library/Heat");
        record.Format = new MediaFormat("avi");
        record.ParsedReleaseYear = new ReleaseYear(1995, Now);
        record.IsInWatchlist = true;
        record.IsWatched = true;
        record.Review = Review.Disliked;
        record.Status = EnrichmentStatus.Enriched;
        record.EnrichedAt = Now;
        record.EnrichmentAttempts = 3;
        record.MetadataTitle = "Heat";
        record.Plot = "A crew of thieves.";
        record.ReleaseYear = new ReleaseYear(1995, Now);
        record.RuntimeMinutes = new Runtime(170);
        record.ImdbRating = new ImdbRating(8.3m);
        record.ImdbId = new ImdbId("tt0113277");
        record.PosterUrl = "https://example.invalid/heat.jpg";

        await MovieCatalogSeed.AddAsync(db, record, ct);
        record.Id.ShouldNotBeNull();
        return (record.Id, record.LibraryPath);
    }

    private void MarkNotFoundBetweenLoadAndSave(MovieId id)
    {
        using var concurrent = LamuFlixDbContextFactory.OpenContext(fixture.ConnectionString);
        var affected = concurrent.Movies
            .Where(movie => movie.Id == id)
            .ExecuteUpdate(setters => setters
                .SetProperty(movie => movie.Status, EnrichmentStatus.NotFound)
                .SetProperty(movie => movie.EnrichmentAttempts, 9));
        affected.ShouldBe(1);
    }

    private static IMediaLibraryScanner ScannerReturning(LibraryPath folder, string extension)
    {
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.ResolvePrimaryVideo(folder).Returns(
            new PrimaryVideo(new LibraryPath($"{folder.Value}/primary.{extension}"), new MediaFormat(extension)));
        return scanner;
    }

    private LamuFlixDbContext OpenSibling(LamuFlixDbContext source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var connectionString = fixture.ConnectionString;
        connectionString.ShouldNotBeNullOrWhiteSpace();
        return LamuFlixDbContextFactory.OpenContext(connectionString);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(
            db,
            TimeProvider.System,
            Options.Create(new EnrichmentOptions { ClaimLease = ClaimLease }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);
}
