using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Library;

public sealed class BrowseMoviesQueryHandler(IMovieCatalog catalog)
    : IQueryHandler<BrowseMoviesQuery, PagedResult<MovieSummary>>
{
    public Task<PagedResult<MovieSummary>> HandleAsync(
        BrowseMoviesQuery query,
        CancellationToken cancellationToken) =>
        catalog.BrowseAsync(query.Query, cancellationToken);
}
