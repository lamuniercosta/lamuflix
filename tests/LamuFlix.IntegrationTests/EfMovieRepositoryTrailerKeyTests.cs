using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(RepositoryMutationCollection))]
public sealed class EfMovieRepositoryTrailerKeyTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChanges_KeyAbsent_StoresNullAndReadsBackMetadataWithoutTrailerKey()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Trailer Absent", "trailer-absent", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkEnriched(Metadata("Trailer Absent", "tt0000301", trailerKey: null), Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Movies.AsNoTracking().Where(row => row.Id == id)
                .Select(row => row.TrailerKey)
                .SingleAsync(ct))
            .ShouldBeNull();
        var reloaded = await Repository(verify).GetAsync(id, ct);
        reloaded.ShouldNotBeNull();
        reloaded.Metadata.ShouldNotBeNull();
        reloaded.Metadata.TrailerKey.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_PresentKey_RoundTripsThroughRepositoryAndDetailsProjection()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Trailer Present", "trailer-present", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkEnriched(Metadata("Trailer Present", "tt0000302", trailerKey: "dQw4w9WgXcQ"), Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var readContext = fixture.CreateMigratedContext();
        var stored = await readContext.Movies.AsNoTracking().Where(row => row.Id == id)
            .Select(row => row.TrailerKey)
            .SingleAsync(ct);
        stored.ShouldBe("dQw4w9WgXcQ");

        var reloaded = await Repository(readContext).GetAsync(id, ct);
        reloaded.ShouldNotBeNull();
        reloaded.Metadata.ShouldNotBeNull();
        reloaded.Metadata.TrailerKey.ShouldBe("dQw4w9WgXcQ");

        var details = await new EfMovieCatalog(readContext).GetDetailsAsync(id, ct);
        details.ShouldNotBeNull();
        details.Metadata.ShouldNotBeNull();
        details.Metadata.TrailerKey.ShouldBe("dQw4w9WgXcQ");
    }

    [Fact]
    public async Task SaveChanges_ReenrichedWithNewKey_ReplacesTheStoredKey()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Trailer Replace", "trailer-replace", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(Metadata("Trailer Replace", "tt0000303", trailerKey: "dQw4w9WgXcQ"), Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(Metadata("Trailer Replace", "tt0000303", trailerKey: "oHg5SJYRHA0"), Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Movies.AsNoTracking().Where(row => row.Id == id)
                .Select(row => row.TrailerKey)
                .SingleAsync(ct))
            .ShouldBe("oHg5SJYRHA0");
    }

    [Fact]
    public async Task SaveChanges_ReenrichedWithoutEligibleTrailer_ClearsTheStoredKey()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Trailer Clear", "trailer-clear", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(Metadata("Trailer Clear", "tt0000304", trailerKey: "dQw4w9WgXcQ"), Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(Metadata("Trailer Clear", "tt0000304", trailerKey: null), Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Movies.AsNoTracking().Where(row => row.Id == id)
                .Select(row => row.TrailerKey)
                .SingleAsync(ct))
            .ShouldBeNull();
        var reloaded = await Repository(verify).GetAsync(id, ct);
        reloaded.ShouldNotBeNull();
        reloaded.Metadata.ShouldNotBeNull();
        reloaded.Metadata.TrailerKey.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_ClearMetadata_ClearsTheStoredKey()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Trailer Wipe", "trailer-wipe", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(Metadata("Trailer Wipe", "tt0000305", trailerKey: "dQw4w9WgXcQ"), Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.ClearMetadata();
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Movies.AsNoTracking().Where(row => row.Id == id)
                .Select(row => row.TrailerKey)
                .SingleAsync(ct))
            .ShouldBeNull();
    }

    [Fact]
    public async Task GetDetailsAsync_ExistingRowWithoutTrailerKey_ReturnsNullKey()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var context = fixture.CreateMigratedContext();
        var record = MovieCatalogSeed.Create("Legacy Row", "trailer-legacy");
        record.MetadataTitle = "Legacy Row";
        record.Plot = "Legacy plot";
        await MovieCatalogSeed.AddAsync(context, record, ct);
        var id = record.Id;
        id.ShouldNotBeNull();

        // act
        var details = await new EfMovieCatalog(context).GetDetailsAsync(id, ct);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldNotBeNull();
        details.Metadata.TrailerKey.ShouldBeNull();
    }

    [Fact]
    public async Task GetDetailsAsync_TrailerKeyWithoutPrimaryMetadata_StillCountsAsMetadata()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var context = fixture.CreateMigratedContext();
        var record = MovieCatalogSeed.Create("Key Only", "trailer-key-only");
        record.TrailerKey = "dQw4w9WgXcQ";
        await MovieCatalogSeed.AddAsync(context, record, ct);
        var id = record.Id;
        id.ShouldNotBeNull();

        // act
        var details = await new EfMovieCatalog(context).GetDetailsAsync(id, ct);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldNotBeNull();
        details.Metadata.TrailerKey.ShouldBe("dQw4w9WgXcQ");
    }

    private static MovieMetadata Metadata(string title, string imdbId, string? trailerKey) =>
        new(
            title,
            "Full synopsis",
            new ReleaseYear(2024, Now),
            new Runtime(121),
            new ImdbRating(8.1m),
            new ImdbId(imdbId),
            "https://example.test/poster.jpg",
            null,
            82,
            [],
            ["Lead Actor"],
            [],
            [],
            trailerKey: trailerKey);

    private static async Task<MovieId> AddMovieAsync(
        EfMovieRepository repository,
        string title,
        string suffix,
        CancellationToken ct)
    {
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(
            Movie.Create(id, title, new LibraryPath($"C:/library/{suffix}.mkv"), new MediaFormat("mkv")), ct);
        await repository.SaveChangesAsync(ct);
        return id;
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(
            db,
            new FixedTimeProvider(Now),
            Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);
}
