using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class EfMovieRepositoryBulkRescanTests(PostgresFixture fixture) : IAsyncLifetime, IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task HandleAsync_MixedLibrary_UpdatesChangedRowOnlyAndAccountsForEverySnapshotId()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var changed = await SeedAsync(db, "Heat", "avi", ct);
        var unchanged = await SeedAsync(db, "Collateral", "mkv", ct);
        var missingFolder = await SeedAsync(db, "Ronin", "avi", ct);
        var noEligibleVideo = await SeedAsync(db, "Thief", "avi", ct);
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.ResolvePrimaryVideo(changed.Path).Returns(Primary(changed.Path, "mkv"));
        scanner.ResolvePrimaryVideo(unchanged.Path).Returns(Primary(unchanged.Path, "mkv"));
        scanner.ResolvePrimaryVideo(missingFolder.Path).Throws(new NotFoundException());
        scanner.ResolvePrimaryVideo(noEligibleVideo.Path)
            .Throws(new InvalidTransitionException("Library folder contains no eligible top-level video file."));
        var handler = new RescanLibraryCommandHandler(Repository(db), scanner);

        // act
        var result = await handler.HandleAsync(new RescanLibraryCommand(), ct);

        // assert
        result.Changed.ShouldBe(1);
        result.Unchanged.ShouldBe(1);
        result.Failed.ShouldBe(2);
        result.FailedMovieIds.Select(id => id.Value)
            .ShouldBe([missingFolder.Id.Value, noEligibleVideo.Id.Value]);

        await using var verify = OpenSibling(db);
        var rows = await verify.Movies.AsNoTracking().OrderBy(movie => movie.Id).ToArrayAsync(ct);
        rows.Length.ShouldBe(4);
        rows[0].Format.ShouldBe(new MediaFormat("mkv"));
        rows[0].Title.ShouldBe("Heat");
        rows[0].ParsedReleaseYear.ShouldBe(new ReleaseYear(1995, MovieCatalogSeed.FixedTime));
        rows[0].Status.ShouldBe(EnrichmentStatus.Enriched);
        rows[0].MetadataTitle.ShouldBe("Heat");
        rows[1].Format.ShouldBe(new MediaFormat("mkv"));
        rows[2].Format.ShouldBe(new MediaFormat("avi"));
        rows[3].Format.ShouldBe(new MediaFormat("avi"));
    }

    [Fact]
    public async Task HandleAsync_SecondUnchangedPass_WritesNothingAndReportsEveryIdUnchanged()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var first = await SeedAsync(db, "Heat", "mkv", ct);
        var second = await SeedAsync(db, "Collateral", "mp4", ct);
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.ResolvePrimaryVideo(first.Path).Returns(Primary(first.Path, "mkv"));
        scanner.ResolvePrimaryVideo(second.Path).Returns(Primary(second.Path, "mp4"));
        var handler = new RescanLibraryCommandHandler(Repository(db), scanner);

        // act
        var result = await handler.HandleAsync(new RescanLibraryCommand(), ct);

        // assert
        result.Changed.ShouldBe(0);
        result.Unchanged.ShouldBe(2);
        result.Failed.ShouldBe(0);
        result.FailedMovieIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_SecondUnchangedPass_IssuesNoUpdate()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var seeded = await SeedAsync(db, "Heat", "mkv", ct);
        var capture = new ClaimCommandCaptureInterceptor();
        await using var capturing = new LamuFlixDbContext(new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(fixture.ConnectionString, options => options.UseVector())
            .AddInterceptors(capture)
            .Options);
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.ResolvePrimaryVideo(seeded.Path).Returns(Primary(seeded.Path, "mkv"));
        var handler = new RescanLibraryCommandHandler(Repository(capturing), scanner);

        // act
        var result = await handler.HandleAsync(new RescanLibraryCommand(), ct);

        // assert
        result.Unchanged.ShouldBe(1);
        capture.Commands.ShouldNotContain(command =>
            command.Contains("UPDATE", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SnapshotMovieIdsAsync_PopulatedLibrary_ReturnsAscendingIdsOnly()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var third = await SeedAsync(db, "Thief", "avi", ct);
        var first = await SeedAsync(db, "Heat", "avi", ct);
        var second = await SeedAsync(db, "Ronin", "avi", ct);

        // act
        var ids = await Repository(db).SnapshotMovieIdsAsync(ct);

        // assert
        ids.Count.ShouldBe(3);
        ids.Select(id => id.Value).ShouldBe(ids.Select(id => id.Value).Order());
        ids.ShouldContain(first.Id);
        ids.ShouldContain(second.Id);
        ids.ShouldContain(third.Id);
    }

    [Fact]
    public async Task SnapshotMovieIdsAsync_EmptyLibrary_ReturnsEmptyWithoutPreloadingEntities()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();

        // act
        var ids = await Repository(db).SnapshotMovieIdsAsync(ct);

        // assert
        ids.ShouldBeEmpty();
    }

    private static async Task<(MovieId Id, LibraryPath Path)> SeedAsync(
        LamuFlixDbContext db,
        string pathSuffix,
        string extension,
        CancellationToken ct)
    {
        var record = MovieCatalogSeed.Create(pathSuffix, pathSuffix);
        record.Format = new MediaFormat(extension);
        record.Status = EnrichmentStatus.Enriched;
        record.ParsedReleaseYear = new ReleaseYear(1995, MovieCatalogSeed.FixedTime);
        record.MetadataTitle = pathSuffix;
        await MovieCatalogSeed.AddAsync(db, record, ct);
        record.Id.ShouldNotBeNull();
        return (record.Id, record.LibraryPath);
    }

    private static PrimaryVideo Primary(LibraryPath folder, string extension) =>
        new(new LibraryPath($"{folder.Value}/primary.{extension}"), new MediaFormat(extension));

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
