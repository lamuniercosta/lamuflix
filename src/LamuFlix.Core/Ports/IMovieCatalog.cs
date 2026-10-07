using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;

namespace LamuFlix.Core.Ports;

public interface IMovieCatalog
{
    Task<PagedResult<MovieSummary>> BrowseAsync(MovieQuery query, CancellationToken ct);

    Task<MovieDetails?> GetDetailsAsync(MovieId id, CancellationToken ct);

    Task<IReadOnlyList<GenreFacet>> GetGenresAsync(CancellationToken ct);

    Task<IReadOnlyList<PersonFacet>> GetPeopleAsync(CancellationToken ct);
}

public sealed record GenreFacet(int Id, string Name);

public sealed record PersonFacet(int Id, string Name);
