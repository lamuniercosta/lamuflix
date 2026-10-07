using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Library;

public sealed class GetGenresQueryHandler(IMovieCatalog catalog)
    : IQueryHandler<GetGenresQuery, IReadOnlyList<GenreFacet>>
{
    public Task<IReadOnlyList<GenreFacet>> HandleAsync(
        GetGenresQuery query,
        CancellationToken cancellationToken) =>
        catalog.GetGenresAsync(cancellationToken);
}
