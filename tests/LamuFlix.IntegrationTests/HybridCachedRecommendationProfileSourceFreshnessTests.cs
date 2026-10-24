using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class HybridCachedRecommendationProfileSourceFreshnessTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetTasteProfileAsync_BeforeExpiration_ReturnsCachedProfile()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");
        var first = await harness.Adapter.GetTasteProfileAsync(ct);
        var readsAfterFirst = harness.Commands.ReaderCount;

        // act
        harness.Time.Advance(harness.Expiration - TimeSpan.FromTicks(1));
        var second = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        first.ShouldMatchProfile(second);
        harness.Commands.ReaderCount.ShouldBe(readsAfterFirst);
        harness.Scopes.Created.ShouldBe(1);
    }

    [Fact]
    public async Task GetTasteProfileAsync_AtApplicationExpiration_EvictsAndRecomputes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");
        await harness.Adapter.GetTasteProfileAsync(ct);
        var readsAfterFirst = harness.Commands.ReaderCount;
        await harness.SeedMovieAsync("Watched after fill", isWatched: true, ct, "Horror");

        // act
        harness.Time.Advance(harness.Expiration);
        var second = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        second.GenrePreferences.Keys.ShouldBe(["Drama", "Horror"], ignoreOrder: true);
        harness.Commands.ReaderCount.ShouldBeGreaterThan(readsAfterFirst);
        harness.Scopes.Created.ShouldBe(2);
        harness.Scopes.Disposed.ShouldBe(2);
    }

    [Fact]
    public async Task GetTasteProfileAsync_ConcurrentSameKeyCallers_CoalesceOnOneComputation()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");
        harness.Commands.BlockNextReader();

        // act
        var firstPending = harness.Adapter.GetTasteProfileAsync(ct);
        await harness.Commands.WaitUntilBlockedAsync();
        var secondPending = harness.Adapter.GetTasteProfileAsync(ct);
        harness.Commands.ReleaseBlockedReader();
        var first = await firstPending;
        var second = await secondPending;

        // assert
        first.IsEmpty.ShouldBeFalse();
        first.ShouldMatchProfile(second);
        harness.Scopes.Created.ShouldBe(1);
        harness.Scopes.Disposed.ShouldBe(1);
    }

    [Fact]
    public async Task GetTasteProfileAsync_InvalidationDuringInFlightFill_RejectsOldGenerationForPostInvalidationRead()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched before invalidation", isWatched: true, ct, "Drama");
        harness.Commands.BlockNextReader("FROM genres AS g");

        // act
        var preInvalidationPending = harness.Adapter.GetTasteProfileAsync(ct);
        await harness.Commands.WaitUntilBlockedAsync();
        await harness.SeedMovieAsync("Watched after invalidation", isWatched: true, ct, "Sci-Fi");
        await harness.Adapter.InvalidateAsync(ct);
        var postInvalidationPending = harness.Adapter.GetTasteProfileAsync(ct);
        harness.Commands.ReleaseBlockedReader();
        var preInvalidation = await preInvalidationPending;
        var postInvalidation = await postInvalidationPending;

        // assert
        postInvalidation.GenrePreferences.Keys.ShouldBe(["Drama", "Sci-Fi"], ignoreOrder: true);
        preInvalidation.GenrePreferences.Keys.ShouldBe(["Drama", "Sci-Fi"], ignoreOrder: true);
        harness.Scopes.Created.ShouldBeLessThanOrEqualTo(3);
        harness.Scopes.Disposed.ShouldBe(harness.Scopes.Created);
    }

    [Fact]
    public async Task InvalidateAsync_ForcesNextReadToRecompute()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");
        await harness.Adapter.GetTasteProfileAsync(ct);
        var readsAfterFirst = harness.Commands.ReaderCount;

        // act
        await harness.Adapter.InvalidateAsync(ct);
        await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        harness.Commands.ReaderCount.ShouldBeGreaterThan(readsAfterFirst);
        harness.Scopes.Created.ShouldBe(2);
    }

    [Fact]
    public async Task GetTasteProfileAsync_OneWaiterCancels_RemainingWaiterSucceeds()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");
        using var firstCancellation = new CancellationTokenSource();
        harness.Commands.BlockNextReader();

        // act
        var firstPending = harness.Adapter.GetTasteProfileAsync(firstCancellation.Token);
        await harness.Commands.WaitUntilBlockedAsync();
        var secondPending = harness.Adapter.GetTasteProfileAsync(ct);
        await firstCancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(async () => await firstPending);
        harness.Commands.ReleaseBlockedReader();
        var profile = await secondPending;

        // assert
        profile.IsEmpty.ShouldBeFalse();
        harness.Scopes.Created.ShouldBe(1);
        harness.Scopes.Disposed.ShouldBe(1);
        harness.Commands.ResetCount();
        var cached = await harness.Adapter.GetTasteProfileAsync(ct);
        profile.ShouldMatchProfile(cached);
        harness.Commands.ReaderCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetTasteProfileAsync_AllWaitersCancel_DoesNotCacheFallbackAndLaterReadRecovers()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();
        harness.Commands.BlockNextReader();

        // act
        var firstPending = harness.Adapter.GetTasteProfileAsync(firstCancellation.Token);
        await harness.Commands.WaitUntilBlockedAsync();
        var secondPending = harness.Adapter.GetTasteProfileAsync(secondCancellation.Token);
        await firstCancellation.CancelAsync();
        await secondCancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(async () => await firstPending);
        await Should.ThrowAsync<OperationCanceledException>(async () => await secondPending);
        harness.Commands.ReleaseBlockedReader();
        var recovered = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        recovered.IsEmpty.ShouldBeFalse();
        harness.Scopes.Disposed.ShouldBe(harness.Scopes.Created);
    }

    [Fact]
    public async Task GetTasteProfileAsync_FactoryFailure_IsNotCachedAndLaterReadRecovers()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");
        harness.Commands.FailNextReader();

        // act
        var failedRead = harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        await Should.ThrowAsync<InvalidOperationException>(failedRead);
        harness.Scopes.Disposed.ShouldBe(harness.Scopes.Created);
        var recovered = await harness.Adapter.GetTasteProfileAsync(ct);
        recovered.IsEmpty.ShouldBeFalse();
        harness.Scopes.Disposed.ShouldBe(harness.Scopes.Created);
    }

    [Fact]
    public async Task GetTasteProfileAsync_InitiatingScopeDisposes_WhileOwnedScopeWorkContinues()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");
        var requestScope = harness.Services.CreateScope();
        _ = requestScope.ServiceProvider.GetRequiredService<IMovieCatalog>();
        harness.Commands.BlockNextReader();

        // act
        var pending = harness.Adapter.GetTasteProfileAsync(ct);
        await harness.Commands.WaitUntilBlockedAsync();
        await ((IAsyncDisposable)requestScope).DisposeAsync();
        harness.Commands.ReleaseBlockedReader();
        var profile = await pending;

        // assert
        profile.IsEmpty.ShouldBeFalse();
        harness.Scopes.Created.ShouldBe(1);
        harness.Scopes.Disposed.ShouldBe(1);
    }

    [Fact]
    public async Task GetProfileInputsAsync_CreatesAndDisposesFreshOwnedScopePerCall()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("First watched", isWatched: true, ct, "Drama");
        var first = await harness.Adapter.GetProfileInputsAsync([], ct);
        await harness.SeedMovieAsync("Second watched", isWatched: true, ct, "Horror");

        // act
        var second = await harness.Adapter.GetProfileInputsAsync([], ct);

        // assert
        first.Movies.Select(movie => movie.Genres[0]).ShouldBe(["Drama"]);
        second.Movies.Select(movie => movie.Genres[0]).ShouldBe(["Drama", "Horror"], ignoreOrder: true);
        harness.Scopes.Created.ShouldBe(2);
        harness.Scopes.Disposed.ShouldBe(2);
    }

    [Fact]
    public async Task GetProfileInputsAsync_PropagatesCallerCancellationWithoutCreatingScope()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched", isWatched: true, ct, "Drama");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var cancelledToken = cancellation.Token;

        // act
        var cancelledRead = harness.Adapter.GetProfileInputsAsync([], cancelledToken);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(cancelledRead);
        harness.Scopes.Created.ShouldBe(0);
    }
}
