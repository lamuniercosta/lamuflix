using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class ReviewTasteProfileFreshnessTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SaveChangesAsync_AssignLikedReview_InvalidatesAndNextProfileReadReflectsIt()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);
        var target = await harness.SeedMovieAsync("Unplayed horror", isWatched: false, isNotInterested: false, ["Horror"], ct);

        var before = await harness.ReadProfileAsync(ct);
        before.Liked.Genres.ShouldBeEmpty();
        before.Disliked.Genres.ShouldBeEmpty();

        // act
        await harness.UpdateMovieAsync(target, movie => movie.AssignReview(Review.Liked), ct);
        var after = await harness.ReadProfileAsync(ct);

        // assert
        after.Liked.Genres.Keys.ShouldBe(["Horror"]);
        after.GenrePreferences.Keys.ShouldBe(["Drama"]);
    }

    [Fact]
    public async Task SaveChangesAsync_AssignDislikedReview_InvalidatesAndExcludesFromBaseline()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);
        var target = await harness.SeedMovieAsync("Watched horror", isWatched: true, isNotInterested: false, ["Horror"], ct);

        var before = await harness.ReadProfileAsync(ct);
        before.GenrePreferences.Keys.ShouldBe(["Drama", "Horror"], ignoreOrder: true);

        // act
        await harness.UpdateMovieAsync(target, movie => movie.AssignReview(Review.Disliked), ct);
        var after = await harness.ReadProfileAsync(ct);

        // assert
        after.GenrePreferences.Keys.ShouldBe(["Drama"]);
        after.Disliked.Genres.Keys.ShouldBe(["Horror"]);
    }

    [Fact]
    public async Task SaveChangesAsync_ClearReview_RemovesThePreviousSignal()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        var target = await harness.SeedMovieAsync("Unplayed comedy", isWatched: false, isNotInterested: false, ["Comedy"], ct);
        await harness.UpdateMovieAsync(target, movie => movie.AssignReview(Review.Liked), ct);
        var liked = await harness.ReadProfileAsync(ct);
        liked.Liked.Genres.Keys.ShouldBe(["Comedy"]);

        // act
        await harness.UpdateMovieAsync(target, movie => movie.AssignReview(Review.None), ct);
        var cleared = await harness.ReadProfileAsync(ct);

        // assert
        cleared.Liked.Genres.ShouldBeEmpty();
        cleared.Disliked.Genres.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_SwitchLikedToDisliked_ReplacesTheSignal()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        var target = await harness.SeedMovieAsync("Unplayed comedy", isWatched: false, isNotInterested: false, ["Comedy"], ct);
        await harness.UpdateMovieAsync(target, movie => movie.AssignReview(Review.Liked), ct);

        // act
        await harness.UpdateMovieAsync(target, movie => movie.AssignReview(Review.Disliked), ct);
        var switched = await harness.ReadProfileAsync(ct);

        // assert
        switched.Liked.Genres.ShouldBeEmpty();
        switched.Disliked.Genres.Keys.ShouldBe(["Comedy"]);
    }

    [Fact]
    public async Task SaveChangesAsync_LegacyUnmarkNotInterested_RemovesTheDislikeSignal()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        var target = await harness.SeedMovieAsync("Legacy horror", isWatched: false, isNotInterested: true, ["Horror"], ct);
        var before = await harness.ReadProfileAsync(ct);
        before.Disliked.Genres.Keys.ShouldBe(["Horror"]);

        // act
        await harness.UpdateMovieAsync(target, movie => movie.UnmarkNotInterested(), ct);
        var after = await harness.ReadProfileAsync(ct);

        // assert
        after.Disliked.Genres.ShouldBeEmpty();
    }

    [Fact]
    public async Task InvalidateAsync_FailedTagRemoval_NextReadStillRecomputesFreshState()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);

        var warm = await harness.ReadProfileAsync(ct);
        warm.GenrePreferences.Keys.ShouldBe(["Drama"]);

        var target = await harness.SeedMovieAsync("Unplayed comedy", isWatched: false, isNotInterested: false, ["Comedy"], ct);

        // act
        await harness.UpdateMovieAsync(target, movie => movie.AssignReview(Review.Liked), ct);
        var after = await harness.ReadProfileAsync(ct);

        // assert
        after.Liked.Genres.Keys.ShouldBe(["Comedy"]);
        after.GenrePreferences.Keys.ShouldBe(["Drama"]);
    }

    [Fact]
    public async Task FailedRemoveByTagAsync_ThenRead_StillRejectsTheStaleGeneration()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, ct, "Drama");
        var cache = new FailOnceTagRemovalCache(harness.Services.GetRequiredService<HybridCache>());
        var adapter = new HybridCachedRecommendationProfileSource(
            cache,
            harness.Services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(new TasteProfileCacheOptions { Expiration = TimeSpan.FromMinutes(5) }));

        var warm = await adapter.GetTasteProfileAsync(ct);
        warm.GenrePreferences.Keys.ShouldBe(["Drama"]);
        await harness.SeedMovieAsync("Watched comedy", isWatched: true, ct, "Comedy");

        // act
        await Should.ThrowAsync<InvalidOperationException>(() => adapter.InvalidateAsync(ct));
        var after = await adapter.GetTasteProfileAsync(ct);

        // assert
        after.GenrePreferences.Keys.ShouldContain("Drama");
        after.GenrePreferences.Keys.ShouldContain("Comedy");
    }

    private sealed class FailOnceTagRemovalCache(HybridCache inner) : HybridCache
    {
        private int failuresRemaining = 1;

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            System.Collections.Generic.IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) =>
            inner.GetOrCreateAsync(key, state, factory, options, tags, cancellationToken);

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            System.Collections.Generic.IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) =>
            inner.SetAsync(key, value, options, tags, cancellationToken);

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) =>
            inner.RemoveAsync(key, cancellationToken);

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
            Interlocked.Exchange(ref failuresRemaining, 0) == 1
                ? throw new InvalidOperationException("Injected tag removal failure.")
                : inner.RemoveByTagAsync(tag, cancellationToken);
    }
}
