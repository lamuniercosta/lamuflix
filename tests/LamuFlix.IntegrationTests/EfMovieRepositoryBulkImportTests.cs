using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class EfMovieRepositoryBulkImportTests(PostgresFixture fixture) : IAsyncLifetime, IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SnapshotMoviePathsAsync_PopulatedLibrary_ReturnsPathsInIdOrder()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var first = await SeedAsync(db, "Thief", EnrichmentStatus.Pending, ct);
        var second = await SeedAsync(db, "Heat", EnrichmentStatus.Enriched, ct);
        var third = await SeedAsync(db, "Ronin", EnrichmentStatus.NotFound, ct);

        // act
        var paths = await Repository(db).SnapshotMoviePathsAsync(ct);

        // assert
        paths.Count.ShouldBe(3);
        paths.Select(path => path.Value).ShouldBe([first.Path.Value, second.Path.Value, third.Path.Value]);
    }

    [Fact]
    public async Task SnapshotMoviePathsAsync_EmptyLibrary_ReturnsEmpty()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();

        // act
        var paths = await Repository(db).SnapshotMoviePathsAsync(ct);

        // assert
        paths.ShouldBeEmpty();
    }

    [Fact]
    public async Task FindFailedMovieIdsAsync_MixedStatuses_ReturnsOnlyFailedIds()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        await SeedAsync(db, "PendingFilm", EnrichmentStatus.Pending, ct);
        await SeedAsync(db, "EnrichedFilm", EnrichmentStatus.Enriched, ct);
        await SeedAsync(db, "NotFoundFilm", EnrichmentStatus.NotFound, ct);
        var failed = await SeedAsync(db, "FailedFilm", EnrichmentStatus.Failed, ct);

        // act
        var ids = await Repository(db).FindFailedMovieIdsAsync(ct);

        // assert
        ids.ShouldBe([failed.Id]);
    }

    [Fact]
    public async Task FindFailedMovieIdsAsync_NoFailed_ReturnsEmpty()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        await SeedAsync(db, "PendingFilm", EnrichmentStatus.Pending, ct);
        await SeedAsync(db, "EnrichedFilm", EnrichmentStatus.Enriched, ct);

        // act
        var ids = await Repository(db).FindFailedMovieIdsAsync(ct);

        // assert
        ids.ShouldBeEmpty();
    }

    private static async Task<(MovieId Id, LibraryPath Path)> SeedAsync(
        LamuFlixDbContext db,
        string pathSuffix,
        EnrichmentStatus status,
        CancellationToken ct)
    {
        var record = MovieCatalogSeed.Create(pathSuffix, pathSuffix);
        record.Status = status;
        record.ParsedReleaseYear = new ReleaseYear(1995, MovieCatalogSeed.FixedTime);
        record.MetadataTitle = pathSuffix;
        await MovieCatalogSeed.AddAsync(db, record, ct);
        record.Id.ShouldNotBeNull();
        return (record.Id, record.LibraryPath);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(
            db,
            TimeProvider.System,
            Options.Create(new EnrichmentOptions { ClaimLease = ClaimLease }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);
}
