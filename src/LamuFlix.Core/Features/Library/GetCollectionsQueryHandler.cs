using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Library;

public sealed class GetCollectionsQueryHandler(IMovieCatalog catalog)
    : IQueryHandler<GetCollectionsQuery, IReadOnlyList<CollectionFacet>>
{
    public Task<IReadOnlyList<CollectionFacet>> HandleAsync(
        GetCollectionsQuery query,
        CancellationToken cancellationToken) =>
        catalog.GetCollectionsAsync(cancellationToken);
}
