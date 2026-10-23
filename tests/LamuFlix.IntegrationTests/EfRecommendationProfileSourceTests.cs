using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfRecommendationProfileSourceTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset FirstLaunch = new(2026, 9, 1, 20, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset SecondLaunch = new(2026, 9, 20, 21, 30, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetProfileInputsAsync_WatchedOnlyMovie_ReturnsWatchedRowWithoutLaunches()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("Watched only", isWatched: true, ct, "Drama", "Sci-Fi");
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationProfileSource(db);

        // act
        var inputs = await source.GetProfileInputsAsync([], ct);

        // assert
        var movie = inputs.Movies.ShouldHaveSingleItem();
        movie.MovieId.ShouldBe(movieId);
        movie.IsWatched.ShouldBeTrue();
        movie.PlayedAt.ShouldBeEmpty();
        movie.Genres.ShouldBe(["Drama", "Sci-Fi"]);
        inputs.CandidateEmbeddings.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetProfileInputsAsync_LaunchOnlyMovie_ReturnsEveryLaunch()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("Launched only", isWatched: false, ct);
        await SeedLaunchAsync(movieId, SecondLaunch, ct);
        await SeedLaunchAsync(movieId, FirstLaunch, ct);
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationProfileSource(db);

        // act
        var inputs = await source.GetProfileInputsAsync([], ct);

        // assert
        var movie = inputs.Movies.ShouldHaveSingleItem();
        movie.MovieId.ShouldBe(movieId);
        movie.IsWatched.ShouldBeFalse();
        movie.PlayedAt.ShouldBe([FirstLaunch, SecondLaunch]);
    }

    [Fact]
    public async Task GetProfileInputsAsync_WatchedMovieWithLaunches_PreservesLaunchesWithoutUndatedSignal()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("Watched and launched", isWatched: true, ct, "Comedy");
        await SeedLaunchAsync(movieId, FirstLaunch, ct);
        await SeedLaunchAsync(movieId, SecondLaunch, ct);
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationProfileSource(db);

        // act
        var inputs = await source.GetProfileInputsAsync([], ct);

        // assert
        var movie = inputs.Movies.ShouldHaveSingleItem();
        movie.IsWatched.ShouldBeTrue();
        movie.PlayedAt.ShouldBe([FirstLaunch, SecondLaunch]);
    }

    [Fact]
    public async Task GetProfileInputsAsync_MovieWithoutWatchedOrLaunch_IsExcluded()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var watchedId = await SeedMovieAsync("Watched", isWatched: true, ct);
        await SeedMovieAsync("Untouched", isWatched: false, ct);
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationProfileSource(db);

        // act
        var inputs = await source.GetProfileInputsAsync([], ct);

        // assert
        inputs.Movies.Select(movie => movie.MovieId).ShouldBe([watchedId]);
    }

    [Fact]
    public async Task GetProfileInputsAsync_Genres_AreMaterializedInOrdinalOrder()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("Genres", isWatched: true, ct, "Sci-Fi", "Drama", "Action");
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationProfileSource(db);

        // act
        var inputs = await source.GetProfileInputsAsync([], ct);

        // assert
        var movie = inputs.Movies.Single(row => row.MovieId == movieId);
        movie.Genres.ShouldBe(["Action", "Drama", "Sci-Fi"]);
    }

    [Fact]
    public async Task GetProfileInputsAsync_CandidateIds_ReturnsOnlyCandidateVectors()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var firstCandidate = await SeedMovieAsync("First candidate", isWatched: false, ct);
        var secondCandidate = await SeedMovieAsync("Second candidate", isWatched: false, ct);
        var otherMovie = await SeedMovieAsync("Other", isWatched: false, ct);
        var firstVector = VariedVector(offset: 0);
        var secondVector = VariedVector(offset: 1000);
        await SeedVectorAsync(firstCandidate, firstVector, ct);
        await SeedVectorAsync(secondCandidate, secondVector, ct);
        await SeedVectorAsync(otherMovie, VariedVector(offset: 2000), ct);
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationProfileSource(db);

        // act
        var inputs = await source.GetProfileInputsAsync([firstCandidate, secondCandidate], ct);

        // assert
        inputs.CandidateEmbeddings.Keys.ShouldBe([firstCandidate, secondCandidate], ignoreOrder: true);
        inputs.CandidateEmbeddings[firstCandidate].SequenceEqual(firstVector).ShouldBeTrue();
        inputs.CandidateEmbeddings[secondCandidate].SequenceEqual(secondVector).ShouldBeTrue();
    }

    [Fact]
    public async Task GetProfileInputsAsync_ContributorVector_RoundTripsIntoPlotEmbedding()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("Contributor", isWatched: true, ct, "Drama");
        var vector = VariedVector(offset: 500);
        await SeedVectorAsync(movieId, vector, ct);
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationProfileSource(db);

        // act
        var inputs = await source.GetProfileInputsAsync([], ct);

        // assert
        var movie = inputs.Movies.ShouldHaveSingleItem();
        movie.PlotEmbedding.Length.ShouldBe(EmbeddingDimensions.Value);
        movie.PlotEmbedding.SequenceEqual(vector).ShouldBeTrue();
    }

    [Fact]
    public async Task GetProfileInputsAsync_MovieWithoutVector_ReturnsEmptyPlotEmbedding()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var launchedId = await SeedMovieAsync("No vector", isWatched: false, ct, "Drama");
        await SeedLaunchAsync(launchedId, FirstLaunch, ct);
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationProfileSource(db);

        // act
        var inputs = await source.GetProfileInputsAsync([launchedId], ct);

        // assert
        var movie = inputs.Movies.ShouldHaveSingleItem();
        movie.PlotEmbedding.IsDefaultOrEmpty.ShouldBeTrue();
        inputs.CandidateEmbeddings.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetProfileInputsAsync_WatchedMoviesFeedTasteProfileCalculator()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("Profile input", isWatched: true, ct, "Drama", "Sci-Fi");
        await SeedVectorAsync(movieId, VariedVector(offset: 300), ct);
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationProfileSource(db);
        var inputs = await source.GetProfileInputsAsync([movieId], ct);
        var calculator = new TasteProfileCalculator(TimeProvider.System, new LamuFlix.Core.Options.RecommendationOptions());
        var weights = GenreWeights.Calculate(new GenreCounts(2, new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            ["Drama"] = 1,
            ["Sci-Fi"] = 1,
        }));

        // act
        var profile = calculator.Calculate(inputs.Movies, weights);

        // assert
        profile.IsEmpty.ShouldBeFalse();
        profile.GenrePreferences.Keys.ShouldBe(["Drama", "Sci-Fi"], ignoreOrder: true);
        profile.PlotEmbedding.SequenceEqual(inputs.CandidateEmbeddings[movieId]).ShouldBeTrue();
    }

    [Fact]
    public async Task GetProfileInputsAsync_CancelledToken_PropagatesCancellation()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedMovieAsync("Watched", isWatched: true, ct);
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationProfileSource(db);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // act
        var act = () => source.GetProfileInputsAsync([], cancelled.Token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    private async Task<MovieId> SeedMovieAsync(
        string title,
        bool isWatched,
        CancellationToken ct,
        params string[] genres)
    {
        await using var db = fixture.CreateMigratedContext();
        var record = new MovieRecord
        {
            Title = title,
            LibraryPath = new LibraryPath($"C:/library/{Guid.NewGuid():N}.mkv"),
            Format = new MediaFormat("mkv"),
            IsWatched = isWatched,
            Status = EnrichmentStatus.Enriched,
        };

        foreach (var genre in genres)
        {
            record.Genres.Add(new GenreRecord { Name = genre });
        }

        db.Movies.Add(record);
        await db.SaveChangesAsync(ct);

        return record.Id
            ?? throw new InvalidOperationException("The database did not assign a movie id.");
    }

    private async Task SeedLaunchAsync(MovieId movieId, DateTimeOffset playedAt, CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        db.PlaybackEvents.Add(new PlaybackEventRecord { MovieId = movieId, PlayedAt = playedAt });
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedVectorAsync(MovieId movieId, float[] vector, CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        db.MovieFeatures.Add(new MovieFeaturesRecord
        {
            MovieId = movieId,
            Vector = new Vector(vector),
            ComputedAt = FirstLaunch,
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
