using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class HybridCachedRecommendationProfileSource : IRecommendationProfileSource
{
    private const string CacheKey = "LamuFlix.Recommendations.TasteProfile";

    private readonly HybridCache cache;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan expiration;

    private long generation;

    public HybridCachedRecommendationProfileSource(
        HybridCache cache,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<TasteProfileCacheOptions> options)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        ArgumentNullException.ThrowIfNull(options);

        expiration = options.Value.Expiration;
    }

    public async Task<TasteProfile> GetTasteProfileAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        while (true)
        {
            var entry = await cache.GetOrCreateAsync(
                CacheKey,
                CreateEntryAsync,
                tags: [CacheKey],
                cancellationToken: ct);

            ct.ThrowIfCancellationRequested();

            if (!IsStale(entry))
            {
                return entry.Profile;
            }

            await cache.RemoveAsync(CacheKey, cancellationToken: ct);
        }
    }

    public async Task<RecommendationProfileInputs> GetProfileInputsAsync(
        IReadOnlyCollection<MovieId> candidateMovieIds,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(candidateMovieIds);
        ct.ThrowIfCancellationRequested();

        var uncachedSource = ResolveUncachedSource(out var scope);
        await using (scope)
        {
            return await uncachedSource.GetProfileInputsAsync(candidateMovieIds, ct);
        }
    }

    internal async Task InvalidateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        Interlocked.Increment(ref generation);

        await cache.RemoveByTagAsync(CacheKey, ct);
    }

    private async ValueTask<CachedProfile> CreateEntryAsync(CancellationToken factoryToken)
    {
        var generationSnapshot = Interlocked.Read(ref generation);
        var createdAt = timeProvider.GetUtcNow();

        var uncachedSource = ResolveUncachedSource(out var scope);
        await using (scope)
        {
            var profile = await uncachedSource.GetTasteProfileAsync(factoryToken);

            factoryToken.ThrowIfCancellationRequested();

            return new CachedProfile(profile, generationSnapshot, createdAt);
        }
    }

    private EfRecommendationProfileSource ResolveUncachedSource(out AsyncServiceScope scope)
    {
        scope = scopeFactory.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<EfRecommendationProfileSource>();
    }

    private bool IsStale(CachedProfile entry) =>
        entry.Generation != Interlocked.Read(ref generation)
        || timeProvider.GetUtcNow() - entry.CreatedAt >= expiration;

    private sealed record CachedProfile(TasteProfile Profile, long Generation, DateTimeOffset CreatedAt);
}
