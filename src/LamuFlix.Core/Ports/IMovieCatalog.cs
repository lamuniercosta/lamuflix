using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;

namespace LamuFlix.Core.Ports;

public interface IMovieCatalog
{
    Task<PagedResult<MovieSummary>> BrowseAsync(MovieQuery query, CancellationToken ct);

    Task<MovieDetails?> GetDetailsAsync(MovieId id, CancellationToken ct);
}
