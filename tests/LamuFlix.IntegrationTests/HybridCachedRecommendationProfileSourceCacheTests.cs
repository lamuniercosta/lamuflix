using System;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
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
        profile.GenrePreferences.Keys.ShouldBe(["Drama"]);
        firstInputs.CandidateEmbeddings.Keys.ShouldBe([firstCandidate, secondCandidate], ignoreOrder: true);
        secondInputs.CandidateEmbeddings.Keys.ShouldBe([firstCandidate, secondCandidate], ignoreOrder: true);
        readsAfterFirstInputs.ShouldBeGreaterThan(readsAfterProfile);
        readsAfterSecondInputs.ShouldBeGreaterThan(readsAfterFirstInputs);
        profile.ShouldMatchProfile(repeatedProfile);
        harness.Commands.ReaderCount.ShouldBe(readsAfterSecondInputs);
    }

    [Fact]
    public async Task GetTasteProfileAsync_RealAdmission_SerializesThroughTheRegisteredSingletonEnvelope()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        var watchedId = await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama", "Sci-Fi");
        var watchedVector = TasteProfileCacheHarness.VariedVector(offset: 700);
        await harness.SeedVectorAsync(watchedId, watchedVector, ct);

        // act
        var first = await harness.Adapter.GetTasteProfileAsync(ct);
        var serializeCountAfterFirst = harness.Serializer.SerializeCount;
        var observedBytes = harness.Serializer.LastSerializedBytes;
        var second = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        serializeCountAfterFirst.ShouldBe(2);
        harness.Serializer.LastSerializedBytes.ShouldBe(observedBytes);
        harness.Serializer.SerializeCount.ShouldBe(serializeCountAfterFirst);
        harness.Serializer.DeserializeCount.ShouldBeGreaterThanOrEqualTo(1);
        ReferenceEquals(harness.Adapter.Serializer, harness.Serializer).ShouldBeTrue();
        first.GenrePreferences.Keys.ShouldBe(["Drama", "Sci-Fi"], ignoreOrder: true);
        observedBytes.ShouldBeGreaterThan(0);
        first.ShouldMatchProfile(second);
    }

    [Fact]
    public async Task GetTasteProfileAsync_LegacyNamespaceEntry_IsIgnoredAndRebuiltUnderTheCompactKey()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");
        var cache = harness.Services.GetRequiredService<HybridCache>();
        var legacy = new CachedProfile(LegacyProfile(), 11, default);
        await cache.SetAsync(
            "LamuFlix.Recommendations.TasteProfile",
            legacy,
            cancellationToken: ct);
        var serializeCountAfterSeeding = harness.Serializer.SerializeCount;

        // act
        var profile = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        profile.GenrePreferences.Keys.ShouldBe(["Drama"]);
        legacy.Profile.GenrePreferences.Keys.ShouldBe(["Legacy Genre"]);
        harness.Serializer.SerializeCount.ShouldBeGreaterThan(serializeCountAfterSeeding);
        var legacyReadBack = await cache.GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile",
            _ => ValueTask.FromResult(new CachedProfile(LegacyProfile(), -1, default)),
            cancellationToken: ct);
        legacyReadBack.Generation.ShouldBe(legacy.Generation);
        legacyReadBack.Profile.GenrePreferences.Keys.ShouldBe(["Legacy Genre"]);
    }

    [Fact]
    public async Task GetTasteProfileAsync_CompactProfile_SerializesOnConstructionAndReusesWithoutReSerializing()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileCacheHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched profile history", isWatched: true, ct, "Drama");

        // act
        var first = await harness.Adapter.GetTasteProfileAsync(ct);
        var serializeCountAfterFirst = harness.Serializer.SerializeCount;
        var observedBytes = harness.Serializer.LastSerializedBytes;
        var second = await harness.Adapter.GetTasteProfileAsync(ct);

        // assert
        serializeCountAfterFirst.ShouldBe(2);
        harness.Serializer.SerializeCount.ShouldBe(serializeCountAfterFirst);
        harness.Serializer.LastSerializedBytes.ShouldBe(observedBytes);
        ((long)harness.Serializer.LastSerializedBytes).ShouldBeLessThan(
            new HybridCacheOptions().MaximumPayloadBytes);
        first.ShouldMatchProfile(second);
    }

    private static TasteProfile LegacyProfile()
    {
        var genres = System.Collections.Immutable.ImmutableDictionary
            .Create<string, double>(StringComparer.OrdinalIgnoreCase)
            .Add("Legacy Genre", 1.0);

        return new TasteProfile(genres);
    }
}
