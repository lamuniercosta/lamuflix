using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class PersistenceRoundTripTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset EnrichedAtUtc =
        new(2026, 3, 15, 12, 30, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset LastAttemptAtUtc =
        new(2026, 3, 16, 8, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SaveChanges_MovieWithRelatedSets_AssignsStoreIdAndRoundTrips()
    {
        await using var write = fixture.CreateMigratedContext();
        var movie = FullyPopulatedMovie("C:/library/incoming/round-trip.mkv");
        write.Movies.Add(movie);

        await write.SaveChangesAsync(TestContext.Current.CancellationToken);

        var movieId = movie.Id;
        movieId.ShouldNotBeNull();
        movieId.Value.ShouldBeGreaterThan(0);
        var id = movieId.Value;

        await using var read = OpenSibling(write);
        var loaded = await LoadMovieAsync(read, id, TestContext.Current.CancellationToken);

        loaded.Title.ShouldBe(movie.Title);
        loaded.LibraryPath.Value.ShouldBe(movie.LibraryPath.Value);
        loaded.Format.Extension.ShouldBe(movie.Format.Extension);
        loaded.IsInWatchlist.ShouldBeTrue();
        loaded.Status.ShouldBe(EnrichmentStatus.Enriched);
        loaded.EnrichedAt.ShouldBe(EnrichedAtUtc);
        loaded.EnrichmentAttempts.ShouldBe(2);
        loaded.EnrichmentFailureCategory.ShouldBe(EnrichmentFailureCategory.RateLimited);
        loaded.LastAttemptAt.ShouldBe(LastAttemptAtUtc);
        loaded.MetadataTitle.ShouldBe("The Round Trip");
        var runtimeMinutes = loaded.RuntimeMinutes;
        runtimeMinutes.ShouldNotBeNull();
        runtimeMinutes.Minutes.ShouldBe(128);
        var releaseYear = loaded.ReleaseYear;
        releaseYear.ShouldNotBeNull();
        releaseYear.Value.ShouldBe(2024);
        var imdbRating = loaded.ImdbRating;
        imdbRating.ShouldNotBeNull();
        imdbRating.Value.ShouldBe(8.5m);
        var imdbId = loaded.ImdbId;
        imdbId.ShouldNotBeNull();
        imdbId.Value.ShouldBe("tt1234567");
        loaded.RottenTomatoesRating.ShouldBe((short)91);
        loaded.MetaScore.ShouldBe((short)82);
        loaded.Plot.ShouldBe("A plot.");
        loaded.PosterUrl.ShouldBe("https://example.test/poster.jpg");
        loaded.Actors.Select(actor => actor.Name).ShouldBe(["Ada Actor"]);
        loaded.Directors.Select(director => director.Name).ShouldBe(["Dana Director"]);
        loaded.Genres.Select(genre => genre.Name).ShouldBe(["Drama"]);
    }

    [Fact]
    public async Task SaveChanges_UtcTimestamps_RoundTripWithZeroOffset()
    {
        await using var write = fixture.CreateMigratedContext();
        var movie = TitleOnlyMovie("C:/library/incoming/utc.mkv");
        movie.EnrichedAt = EnrichedAtUtc;
        movie.LastAttemptAt = LastAttemptAtUtc;
        write.Movies.Add(movie);
        await write.SaveChangesAsync(TestContext.Current.CancellationToken);
        var movieId = movie.Id;
        movieId.ShouldNotBeNull();
        var id = movieId.Value;

        await using var read = OpenSibling(write);
        var loaded = await LoadMovieAsync(read, id, TestContext.Current.CancellationToken);

        var enrichedAt = loaded.EnrichedAt;
        enrichedAt.ShouldNotBeNull();
        enrichedAt.Value.Offset.ShouldBe(TimeSpan.Zero);
        enrichedAt.ShouldBe(EnrichedAtUtc);
        var lastAttemptAt = loaded.LastAttemptAt;
        lastAttemptAt.ShouldNotBeNull();
        lastAttemptAt.Value.Offset.ShouldBe(TimeSpan.Zero);
        lastAttemptAt.ShouldBe(LastAttemptAtUtc);
    }

    [Fact]
    public async Task SaveChanges_AllNullMetadata_RoundTripsAsNull()
    {
        await using var write = fixture.CreateMigratedContext();
        var movie = BaseMovie("C:/library/incoming/null-meta.mkv");
        write.Movies.Add(movie);
        await write.SaveChangesAsync(TestContext.Current.CancellationToken);
        var movieId = movie.Id;
        movieId.ShouldNotBeNull();
        var id = movieId.Value;

        await using var read = OpenSibling(write);
        var loaded = await LoadMovieAsync(read, id, TestContext.Current.CancellationToken);

        loaded.MetadataTitle.ShouldBeNull();
        loaded.RuntimeMinutes.ShouldBeNull();
        loaded.ReleaseYear.ShouldBeNull();
        loaded.ImdbRating.ShouldBeNull();
        loaded.ImdbId.ShouldBeNull();
        loaded.RottenTomatoesRating.ShouldBeNull();
        loaded.MetaScore.ShouldBeNull();
        loaded.Plot.ShouldBeNull();
        loaded.PosterUrl.ShouldBeNull();
        loaded.EnrichedAt.ShouldBeNull();
        loaded.EnrichmentFailureCategory.ShouldBeNull();
        loaded.LastAttemptAt.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_TitleOnlyMetadata_RoundTrips()
    {
        await using var write = fixture.CreateMigratedContext();
        var movie = TitleOnlyMovie("C:/library/incoming/title-only.mkv");
        write.Movies.Add(movie);
        await write.SaveChangesAsync(TestContext.Current.CancellationToken);
        var movieId = movie.Id;
        movieId.ShouldNotBeNull();
        var id = movieId.Value;

        await using var read = OpenSibling(write);
        var loaded = await LoadMovieAsync(read, id, TestContext.Current.CancellationToken);

        loaded.MetadataTitle.ShouldBe("Title Only");
        loaded.RuntimeMinutes.ShouldBeNull();
        loaded.ReleaseYear.ShouldBeNull();
        loaded.ImdbRating.ShouldBeNull();
        loaded.ImdbId.ShouldBeNull();
        loaded.RottenTomatoesRating.ShouldBeNull();
        loaded.MetaScore.ShouldBeNull();
        loaded.Plot.ShouldBeNull();
        loaded.PosterUrl.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_SecondMovieWithNullImdbId_Succeeds()
    {
        await using var write = fixture.CreateMigratedContext();
        write.Movies.Add(BaseMovie("C:/library/incoming/first-null-imdb.mkv"));
        write.Movies.Add(BaseMovie("C:/library/incoming/second-null-imdb.mkv"));

        await write.SaveChangesAsync(TestContext.Current.CancellationToken);

        write.Movies.Count(movie => movie.ImdbId == null).ShouldBe(2);
    }

    private static LamuFlixDbContext OpenSibling(LamuFlixDbContext source)
    {
        var connectionString = source.Database.GetConnectionString();
        connectionString.ShouldNotBeNullOrWhiteSpace();
        return LamuFlixDbContextFactory.OpenContext(connectionString);
    }

    private static Task<MovieRecord> LoadMovieAsync(
        LamuFlixDbContext context,
        int id,
        CancellationToken cancellationToken) =>
        context.Movies
            .Include(movie => movie.Actors)
            .Include(movie => movie.Directors)
            .Include(movie => movie.Genres)
            .SingleAsync(movie => movie.Id == new MovieId(id), cancellationToken);

    private static MovieRecord FullyPopulatedMovie(string libraryPath)
    {
        var movie = TitleOnlyMovie(libraryPath);
        movie.Status = EnrichmentStatus.Enriched;
        movie.IsInWatchlist = true;
        movie.EnrichedAt = EnrichedAtUtc;
        movie.EnrichmentAttempts = 2;
        movie.EnrichmentFailureCategory = EnrichmentFailureCategory.RateLimited;
        movie.LastAttemptAt = LastAttemptAtUtc;
        movie.MetadataTitle = "The Round Trip";
        movie.RuntimeMinutes = new Runtime(128);
        movie.ReleaseYear = new ReleaseYear(2024, new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero));
        movie.ImdbRating = new ImdbRating(8.5m);
        movie.ImdbId = new ImdbId("tt1234567");
        movie.RottenTomatoesRating = 91;
        movie.MetaScore = 82;
        movie.Plot = "A plot.";
        movie.PosterUrl = "https://example.test/poster.jpg";
        movie.Actors.Add(new ActorRecord { Name = "Ada Actor" });
        movie.Directors.Add(new DirectorRecord { Name = "Dana Director" });
        movie.Genres.Add(new GenreRecord { Name = "Drama" });
        return movie;
    }

    private static MovieRecord TitleOnlyMovie(string libraryPath)
    {
        var movie = BaseMovie(libraryPath);
        movie.MetadataTitle = "Title Only";
        return movie;
    }

    private static MovieRecord BaseMovie(string libraryPath) =>
        new()
        {
            Title = "Library Title",
            LibraryPath = new LibraryPath(libraryPath),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending
        };
}
