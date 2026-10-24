using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Tests.Common;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class HybridCachedRecommendationProfileSourceCacheTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetTasteProfileAsync_RepeatedReads_ReuseSingleComputation()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama", "Sci-Fi");

        // act
        var first = await harness.Adapter.GetTasteProfileAsync(ct);
        var readsAfterFirst = harness.Commands.ReaderCount;
        var second = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        first.IsEmpty.ShouldBeFalse();
        first.ShouldMatchProfile(second);
        harness.Commands.ReaderCount.ShouldBe(readsAfterFirst);
        harness.Scopes.Created.ShouldBe(1);
        harness.Scopes.Disposed.ShouldBe(1);
    }

    [Fact]
    public async Task GetTasteProfileAsync_RepeatedReads_ServeTheFilledEntryRatherThanNewPersistenceRows()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");
        var first = await harness.Adapter.GetTasteProfileAsync(ct);

        // act
        await harness.SeedMovieAsync("Newly watched without invalidation", isWatched: true, ct, "Horror");
        var second = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        first.GenrePreferences.Keys.ShouldBe(["Drama"]);
        first.ShouldMatchProfile(second);
    }

    [Fact]
    public async Task GetProfileInputsAsync_RepeatedReads_RemainUncachedBesideTheFilledTasteProfile()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        var watchedId = await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");
        var firstCandidate = await harness.SeedMovieAsync("First candidate", isWatched: false, ct);
        var secondCandidate = await harness.SeedMovieAsync("Second candidate", isWatched: false, ct);
        var watchedVector = TasteProfileCacheHarness.VariedVector(offset: 700);
        await harness.SeedVectorAsync(watchedId, watchedVector, ct);
        await harness.SeedVectorAsync(firstCandidate, TasteProfileCacheHarness.VariedVector(offset: 0), ct);
        await harness.SeedVectorAsync(secondCandidate, TasteProfileCacheHarness.VariedVector(offset: 1000), ct);

        // act
        var profile = await harness.Adapter.GetTasteProfileAsync(ct);
        var readsAfterProfile = harness.Commands.ReaderCount;
        var firstInputs = await harness.Adapter.GetProfileInputsAsync([firstCandidate, secondCandidate], ct);
        var readsAfterFirstInputs = harness.Commands.ReaderCount;
        var secondInputs = await harness.Adapter.GetProfileInputsAsync([firstCandidate, secondCandidate], ct);
        var readsAfterSecondInputs = harness.Commands.ReaderCount;
        var repeatedProfile = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        profile.PlotEmbedding.SequenceEqual(watchedVector).ShouldBeTrue();
        firstInputs.CandidateEmbeddings.Keys.ShouldBe([firstCandidate, secondCandidate], ignoreOrder: true);
        secondInputs.CandidateEmbeddings.Keys.ShouldBe([firstCandidate, secondCandidate], ignoreOrder: true);
        readsAfterFirstInputs.ShouldBeGreaterThan(readsAfterProfile);
        readsAfterSecondInputs.ShouldBeGreaterThan(readsAfterFirstInputs);
        profile.ShouldMatchProfile(repeatedProfile);
        harness.Commands.ReaderCount.ShouldBe(readsAfterSecondInputs);
    }
}
