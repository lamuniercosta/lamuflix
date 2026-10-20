using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Library;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GenreWeightsCalculatorTests
{
    [Fact]
    public void Constructor_NullCatalog_ThrowsArgumentNullException()
    {
        // arrange
        var cache = new GenreWeightsCache();

        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        Should.Throw<ArgumentNullException>(() => new GenreWeightsCalculator(null!, cache));
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        // arrange
        var catalog = Substitute.For<IMovieCatalog>();

        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        Should.Throw<ArgumentNullException>(() => new GenreWeightsCalculator(catalog, null!));
    }

    [Fact]
    public async Task GetAsync_ColdCache_LoadsCountsAndCalculatesWeights()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var catalog = CatalogReturning(10, new Dictionary<int, long> { [1] = 4 });
        var calculator = new GenreWeightsCalculator(catalog, new GenreWeightsCache());

        // act
        var weights = await calculator.GetAsync(ct);

        // assert
        weights.GetWeight(1).ShouldBe(Math.Log(11.0 / 5.0) + 1.0, 1e-12);
        await catalog.Received(1).GetGenreCountsAsync(ct);
    }

    [Fact]
    public async Task GetAsync_SameCacheAcrossScopes_LoadsCatalogOnceAndSharesSnapshot()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var catalog = CatalogReturning(10, new Dictionary<int, long> { [1] = 4 });
        var services = new ServiceCollection();
        services.AddSingleton(new GenreWeightsCache());
        services.AddScoped(_ => catalog);
        services.AddScoped<GenreWeightsCalculator>();
        await using var provider = services.BuildServiceProvider();

        // act
        await using var firstScope = provider.CreateAsyncScope();
        var firstCalculator = firstScope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>();
        var first = await firstCalculator.GetAsync(ct);
        await using var secondScope = provider.CreateAsyncScope();
        var secondCalculator = secondScope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>();
        var second = await secondCalculator.GetAsync(ct);

        // assert
        ReferenceEquals(firstCalculator, secondCalculator).ShouldBeFalse();
        second.ShouldBeSameAs(first);
        await catalog.Received(1).GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_CachedSnapshot_SecondCallOnANewCalculatorSkipsTheCatalog()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = new GenreWeightsCache();
        var firstCatalog = CatalogReturning(10, new Dictionary<int, long> { [1] = 2 });
        var first = await new GenreWeightsCalculator(firstCatalog, cache).GetAsync(ct);
        var secondCatalog = Substitute.For<IMovieCatalog>();

        // act
        var second = await new GenreWeightsCalculator(secondCatalog, cache).GetAsync(ct);

        // assert
        second.ShouldBeSameAs(first);
        await secondCatalog.DidNotReceiveWithAnyArgs().GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_CancelledToken_ThrowsWithoutCallingTheCatalog()
    {
        // arrange
        var catalog = Substitute.For<IMovieCatalog>();
        var calculator = new GenreWeightsCalculator(catalog, new GenreWeightsCache());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // act
        await Should.ThrowAsync<OperationCanceledException>(() => calculator.GetAsync(cancellation.Token));

        // assert
        await catalog.DidNotReceiveWithAnyArgs().GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_CallerToken_IsPassedToTheCatalog()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        var catalog = CatalogReturning(10, new Dictionary<int, long> { [1] = 2 });
        var calculator = new GenreWeightsCalculator(catalog, new GenreWeightsCache());

        // act
        await calculator.GetAsync(cancellation.Token);

        // assert
        await catalog.Received(1).GetGenreCountsAsync(cancellation.Token);
    }

    [Fact]
    public async Task GetAsync_AfterInvalidation_RebuildUsesTheCurrentScopedCatalog()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = new GenreWeightsCache();
        var firstCatalog = CatalogReturning(10, new Dictionary<int, long> { [1] = 9 });
        var first = await new GenreWeightsCalculator(firstCatalog, cache).GetAsync(ct);
        var secondCatalog = CatalogReturning(10, new Dictionary<int, long> { [2] = 1 });

        // act
        cache.Invalidate();
        var second = await new GenreWeightsCalculator(secondCatalog, cache).GetAsync(ct);

        // assert
        ReferenceEquals(first, second).ShouldBeFalse();
        first.GetWeight(1).ShouldBeGreaterThan(1.0);
        second.GetWeight(2).ShouldBeGreaterThan(1.0);
        await firstCatalog.Received(1).GetGenreCountsAsync(Arg.Any<CancellationToken>());
        await secondCatalog.Received(1).GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_AfterTheCall_DoesNotLeaveTheScopedCatalogOrCalculatorReachableFromTheCache()
    {
        // arrange
        var cache = new GenreWeightsCache();

        // act
        var references = await LoadAndForgetScopedServicesAsync(cache);
        ForceCollection();

        // assert
        references.Calculator.IsAlive.ShouldBeFalse(
            "the singleton cache must not retain the scoped calculator or its loader");
        references.Catalog.IsAlive.ShouldBeFalse(
            "the singleton cache must not retain the scoped catalog");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference Calculator, WeakReference Catalog)> LoadAndForgetScopedServicesAsync(
        GenreWeightsCache cache)
    {
        var catalog = new FakeMovieCatalog(new GenreCounts(10, new Dictionary<int, long> { [1] = 1 }));
        var calculator = new GenreWeightsCalculator(catalog, cache);
        await calculator.GetAsync(TestContext.Current.CancellationToken);
        return (new WeakReference(calculator), new WeakReference(catalog));
    }

    private static IMovieCatalog CatalogReturning(long totalMovies, IReadOnlyDictionary<int, long> moviesWithGenre)
    {
        var catalog = Substitute.For<IMovieCatalog>();
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>())
            .Returns(new GenreCounts(totalMovies, moviesWithGenre));
        return catalog;
    }

    private static void ForceCollection()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private sealed class FakeMovieCatalog(GenreCounts counts) : IMovieCatalog
    {
        public Task<PagedResult<MovieSummary>> BrowseAsync(MovieQuery query, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<MovieDetails?> GetDetailsAsync(MovieId id, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<GenreFacet>> GetGenresAsync(CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<GenreCounts> GetGenreCountsAsync(CancellationToken ct) => Task.FromResult(counts);

        public Task<IReadOnlyList<PersonFacet>> GetPeopleAsync(CancellationToken ct) =>
            throw new NotSupportedException();
    }
}
