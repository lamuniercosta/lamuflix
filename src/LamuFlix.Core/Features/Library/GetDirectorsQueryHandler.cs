using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Library;

public sealed class GetDirectorsQueryHandler(IMovieCatalog catalog)
    : IQueryHandler<GetDirectorsQuery, IReadOnlyList<DirectorFacet>>
{
    public Task<IReadOnlyList<DirectorFacet>> HandleAsync(
        GetDirectorsQuery query,
        CancellationToken cancellationToken) =>
        catalog.GetDirectorsAsync(cancellationToken);
}
