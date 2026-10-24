using System;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Tests.Common;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class HybridCachedRecommendationProfileSourceColdTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetTasteProfileAsync_ColdMissWithoutHistory_ComputesAndReturnsEmptyProfile()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);

        // act
        var profile = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        profile.IsEmpty.ShouldBeTrue();
        harness.Commands.ReaderCount.ShouldBeGreaterThan(0);
        harness.Scopes.Created.ShouldBe(1);
    }

    [Fact]
    public async Task GetTasteProfileAsync_ColdMissWithHistory_ComputesNonEmptyProfile()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        var movieId = await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama", "Sci-Fi");
        var vector = TasteProfileCacheHarness.VariedVector(offset: 300);
        await harness.SeedVectorAsync(movieId, vector, ct);

        // act
        var profile = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        profile.IsEmpty.ShouldBeFalse();
        profile.GenrePreferences.Keys.ShouldBe(["Drama", "Sci-Fi"], ignoreOrder: true);
        profile.PlotEmbedding.SequenceEqual(vector).ShouldBeTrue();
    }

    [Fact]
    public async Task GetTasteProfileAsync_ColdMissWithLaunchOnlyHistory_ComputesNonEmptyGenreProfile()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        var movieId = await harness.SeedMovieAsync("Launched profile history", isWatched: false, ct, "Comedy");
        await harness.SeedLaunchAsync(movieId, new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero), ct);

        // act
        var profile = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        profile.IsEmpty.ShouldBeFalse();
        profile.GenrePreferences.Keys.ShouldBe(["Comedy"]);
        profile.GenrePreferences.Values.ShouldAllBe(weight => weight > 0);
    }

    [Fact]
    public async Task GetTasteProfileAsync_ColdMissWithHistory_UsesFactoryOwnedScopeOnly()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");

        // act
        await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        harness.Scopes.Created.ShouldBe(1);
        harness.Scopes.Disposed.ShouldBe(1);
    }
}
