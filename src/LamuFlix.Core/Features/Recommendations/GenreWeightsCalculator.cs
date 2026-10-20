using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class GenreWeightsCalculator
{
    private readonly IMovieCatalog _catalog;
    private readonly GenreWeightsCache _cache;

    public GenreWeightsCalculator(IMovieCatalog catalog, GenreWeightsCache cache)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(cache);

        _catalog = catalog;
        _cache = cache;
    }

    public Task<GenreWeights> GetAsync(CancellationToken ct) =>
        _cache.GetOrLoadAsync(LoadAsync, ct);

    private async Task<GenreWeights> LoadAsync(CancellationToken ct)
    {
        var counts = await _catalog.GetGenreCountsAsync(ct);
        return GenreWeights.Calculate(counts);
    }
}
