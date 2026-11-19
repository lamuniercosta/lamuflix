using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class KeywordWeightsCalculator
{
    private readonly IMovieCatalog _catalog;
    private readonly KeywordWeightsCache _cache;

    public KeywordWeightsCalculator(IMovieCatalog catalog, KeywordWeightsCache cache)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(cache);
        _catalog = catalog;
        _cache = cache;
    }

    public Task<KeywordCounts> GetAsync(CancellationToken ct) => _cache.GetOrLoadAsync(_catalog.GetKeywordCountsAsync, ct);
}