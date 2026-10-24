using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class FeedbackTasteProfileInvalidationTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SaveChangesAsync_MarkWatched_InvalidatesAndNextDerivedProfileReadRecomputes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);
        var target = await harness.SeedMovieAsync("Unwatched horror", isWatched: false, isNotInterested: false, ["Horror"], ct);

        var before = await harness.ReadProfileAsync(ct);
        before.GenrePreferences.Keys.ShouldBe(["Drama"]);

        // act
        await harness.UpdateMovieAsync(target, movie => movie.MarkWatched(), ct);
        var after = await harness.ReadProfileAsync(ct);

        // assert
        after.GenrePreferences.Keys.ShouldContain("Drama");
        after.GenrePreferences.Keys.ShouldContain("Horror");
    }

    [Fact]
    public async Task SaveChangesAsync_UnmarkWatched_InvalidatesAndNextDerivedProfileReadRecomputes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);
        var target = await harness.SeedMovieAsync("Watched horror", isWatched: true, isNotInterested: false, ["Horror"], ct);

        var before = await harness.ReadProfileAsync(ct);
        before.GenrePreferences.Keys.ShouldBe(["Drama", "Horror"], ignoreOrder: true);

        // act
        await harness.UpdateMovieAsync(target, movie => movie.UnmarkWatched(), ct);
        var after = await harness.ReadProfileAsync(ct);

        // assert
        after.GenrePreferences.Keys.ShouldBe(["Drama"]);
    }

    [Fact]
    public async Task SaveChangesAsync_MarkNotInterested_InvalidatesAndNextDerivedProfileReadRecomputes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);
        var target = await harness.SeedMovieAsync("Unwatched horror", isWatched: false, isNotInterested: false, ["Horror"], ct);

        var before = await harness.ReadProfileAsync(ct);
        before.GenrePreferences.Keys.ShouldBe(["Drama"]);

        await harness.SeedPlaybackAsync(target, ct);

        // act
        await harness.UpdateMovieAsync(target, movie => movie.MarkNotInterested(), ct);
        var after = await harness.ReadProfileAsync(ct);

        // assert
        after.GenrePreferences.Keys.ShouldContain("Drama");
        after.GenrePreferences.Keys.ShouldContain("Horror");
    }

    [Fact]
    public async Task SaveChangesAsync_UnmarkNotInterested_InvalidatesAndNextDerivedProfileReadRecomputes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);
        var target = await harness.SeedMovieAsync("Watched horror", isWatched: true, isNotInterested: true, ["Horror"], ct);

        var before = await harness.ReadProfileAsync(ct);
        before.GenrePreferences.Keys.ShouldBe(["Drama", "Horror"], ignoreOrder: true);

        await harness.SeedMovieAsync("Watched comedy", isWatched: true, isNotInterested: false, ["Comedy"], ct);

        // act
        await harness.UpdateMovieAsync(target, movie => movie.UnmarkNotInterested(), ct);
        var after = await harness.ReadProfileAsync(ct);

        // assert
        after.GenrePreferences.Keys.ShouldContain("Drama");
        after.GenrePreferences.Keys.ShouldContain("Horror");
        after.GenrePreferences.Keys.ShouldContain("Comedy");
    }

    [Fact]
    public async Task SaveChangesAsync_FailedSave_DoesNotInvalidateTheCachedProfile()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);
        var target = await harness.SeedMovieAsync("Unwatched horror", isWatched: false, isNotInterested: false, ["Horror"], ct);

        var before = await harness.ReadProfileAsync(ct);
        before.GenrePreferences.Keys.ShouldBe(["Drama"]);

        await using (var scope = harness.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IMovieRepository>();
            var movie = await repository.GetAsync(target, ct);
            movie.ShouldNotBeNull();

            await harness.DeleteMovieRowAsync(target, ct);
            movie.MarkWatched();

            // act
            await Should.ThrowAsync<DbUpdateConcurrencyException>(() => repository.SaveChangesAsync(ct));
        }

        // assert
        var after = await harness.ReadProfileAsync(ct);
        after.GenrePreferences.Keys.ShouldBe(["Drama"]);
    }

    [Fact]
    public async Task SaveChangesAsync_FailedInvalidation_PropagatesAndKeepsTheDatabaseWrite()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);
        var target = await harness.SeedMovieAsync("Unwatched horror", isWatched: false, isNotInterested: false, ["Horror"], ct);

        using var invalidationCancellation = new CancellationTokenSource();
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(harness.ConnectionString, npgsql => npgsql.UseVector())
            .AddInterceptors(new CancelAfterSaveInterceptor(invalidationCancellation))
            .Options;
        await using var db = new LamuFlixDbContext(options);
        var repository = new EfMovieRepository(
            db,
            TimeProvider.System,
            Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }),
            harness.GenreWeights,
            harness.CachedProfileSource);

        var movie = await repository.GetAsync(target, ct);
        movie.ShouldNotBeNull();
        movie.MarkNotInterested();

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => repository.SaveChangesAsync(invalidationCancellation.Token));

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var persisted = await verify.Movies.SingleAsync(row => row.Id == target, ct);
        persisted.IsNotInterested.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_SuccessfulSave_StillInvalidatesTheGenreWeightsCache()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        var target = await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);

        var initialWeights = await harness.ReadGenreWeightsAsync(ct);
        initialWeights.GetWeight("Drama").ShouldBe(1.0);

        await harness.SeedMovieAsync("Unwatched horror one", isWatched: false, isNotInterested: false, ["Horror"], ct);
        await harness.SeedMovieAsync("Unwatched horror two", isWatched: false, isNotInterested: false, ["Horror"], ct);
        await harness.SeedMovieAsync("Unwatched horror three", isWatched: false, isNotInterested: false, ["Horror"], ct);

        var staleWeights = await harness.ReadGenreWeightsAsync(ct);
        staleWeights.GetWeight("Drama").ShouldBe(1.0);

        // act
        await harness.UpdateMovieAsync(target, movie => movie.MarkNotInterested(), ct);
        var refreshedWeights = await harness.ReadGenreWeightsAsync(ct);

        // assert
        refreshedWeights.GetWeight("Drama").ShouldBe(Math.Log(2.5) + 1.0, 1e-9);
    }
}
