using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class HybridCachedRecommendationProfileSource(IServiceScopeFactory scopeFactory)
    : IRecommendationProfileSource
{
    public async Task<TasteProfile> GetTasteProfileAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await using var scope = scopeFactory.CreateAsyncScope();
        var uncachedSource = scope.ServiceProvider.GetRequiredService<EfRecommendationProfileSource>();

        return await uncachedSource.GetTasteProfileAsync(ct);
    }

    public async Task<RecommendationProfileInputs> GetProfileInputsAsync(
        IReadOnlyCollection<MovieId> candidateMovieIds,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(candidateMovieIds);
        ct.ThrowIfCancellationRequested();

        await using var scope = scopeFactory.CreateAsyncScope();
        var uncachedSource = scope.ServiceProvider.GetRequiredService<EfRecommendationProfileSource>();

        return await uncachedSource.GetProfileInputsAsync(candidateMovieIds, ct);
    }
}
