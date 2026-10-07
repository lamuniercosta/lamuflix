using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Library;

public sealed class GetPeopleQueryHandler(IMovieCatalog catalog)
    : IQueryHandler<GetPeopleQuery, IReadOnlyList<PersonFacet>>
{
    public Task<IReadOnlyList<PersonFacet>> HandleAsync(
        GetPeopleQuery query,
        CancellationToken cancellationToken) =>
        catalog.GetPeopleAsync(cancellationToken);
}
