using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.Options;
using Pgvector;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class ReviewTasteProfileIntegrationTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset ComputedAt = new(2026, 9, 1, 20, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetProfileInputsAsync_UnplayedLikedMovie_LoadsReviewStateAndMetadata()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var liked = await SeedMovieAsync(
            "Unplayed liked",
            Review.Liked,
            isWatched: false,
            ct,
            ["Drama"],
            ["Actor A"],
            ["Director A"]);
        await SeedVectorAsync(liked, VariedVector(0), ct);
        await using var db = fixture.CreateMigratedContext();
        var source = CreateSource(db);

        // act
        var inputs = await source.GetProfileInputsAsync([], ct);

        // assert
        var movie = inputs.Movies.ShouldHaveSingleItem();
        movie.MovieId.ShouldBe(liked);
        movie.Review.ShouldBe(Review.Liked);
        movie.Title.ShouldBe("Unplayed liked");
        movie.Genres.ShouldBe(["Drama"]);
        movie.Cast.ShouldBe(["Actor A"]);
        movie.Directors.ShouldBe(["Director A"]);
        movie.PlotEmbedding.IsEmpty.ShouldBeFalse();
        movie.IsWatched.ShouldBeFalse();
        movie.PlayedAt.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetProfileInputsAsync_UnplayedDislikedMovie_LoadsReviewStateAndMetadata()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedMovieAsync(
            "Unplayed disliked",
            Review.Disliked,
            isWatched: false,
            ct,
            ["Horror"],
            ["Actor B"],
            ["Director B"]);
        await using var db = fixture.CreateMigratedContext();
        var source = CreateSource(db);

        // act
        var inputs = await source.GetProfileInputsAsync([], ct);

        // assert
        var movie = inputs.Movies.ShouldHaveSingleItem();
        movie.Review.ShouldBe(Review.Disliked);
        movie.Genres.ShouldBe(["Horror"]);
    }

    [Fact]
    public async Task GetTasteProfileAsync_BaselineSeparation_OmitsDislikedFromPositiveInputs()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedMovieAsync("Watched drama", Review.None, isWatched: true, ct, ["Drama"], [], []);
        await SeedMovieAsync("Watched disliked horror", Review.Disliked, isWatched: true, ct, ["Horror"], [], []);
        await using var db = fixture.CreateMigratedContext();
        var source = CreateSource(db);

        // act
        var profile = await source.GetTasteProfileAsync(ct);

        // assert
        profile.GenrePreferences.Keys.ShouldBe(["Drama"]);
        profile.Disliked.Genres.Keys.ShouldBe(["Horror"]);
        profile.Liked.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task GetTasteProfileAsync_UnreviewedUnwatchedMovie_StaysOutOfBothAggregates()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedMovieAsync("Untouched", Review.None, isWatched: false, ct, ["Western"], [], []);
        await using var db = fixture.CreateMigratedContext();
        var source = CreateSource(db);

        // act
        var profile = await source.GetTasteProfileAsync(ct);

        // assert
        profile.IsEmpty.ShouldBeTrue();
    }

    private static EfRecommendationProfileSource CreateSource(LamuFlixDbContext db) =>
        new(
            db,
            new GenreWeightsCalculator(new EfMovieCatalog(db), new GenreWeightsCache()),
            TimeProvider.System,
            Options.Create(new RecommendationOptions()));

    private async Task<MovieId> SeedMovieAsync(
        string title,
        Review review,
        bool isWatched,
        CancellationToken ct,
        string[] genres,
        string[] actors,
        string[] directors)
    {
        await using var db = fixture.CreateMigratedContext();
        var record = new MovieRecord
        {
            Title = title,
            LibraryPath = new LibraryPath($"C:/library/{Guid.NewGuid():N}.mkv"),
            Format = new MediaFormat("mkv"),
            IsWatched = isWatched,
            Review = review,
            Status = EnrichmentStatus.Enriched,
        };

        foreach (var genre in genres)
        {
            record.Genres.Add(new GenreRecord { Name = genre });
        }

        foreach (var actor in actors)
        {
            record.Actors.Add(new ActorRecord { Name = actor });
        }

        foreach (var director in directors)
        {
            record.Directors.Add(new DirectorRecord { Name = director });
        }

        db.Movies.Add(record);
        await db.SaveChangesAsync(ct);

        return record.Id
            ?? throw new InvalidOperationException("The database did not assign a movie id.");
    }

    private async Task SeedVectorAsync(MovieId movieId, float[] vector, CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        db.MovieFeatures.Add(new MovieFeaturesRecord
        {
            MovieId = movieId,
            Vector = new Vector(vector),
            ComputedAt = ComputedAt,
        });
        await db.SaveChangesAsync(ct);
    }

    private static float[] VariedVector(int offset)
    {
        var vector = new float[EmbeddingDimensions.Value];

        for (var index = 0; index < vector.Length; index++)
        {
            vector[index] = ((index + offset) % 37) / 37f;
        }

        vector[0] = 1f;
        return vector;
    }
}
