using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Library;

public sealed class GetMovieDetailsQueryHandler(IMovieCatalog catalog)
    : IQueryHandler<GetMovieDetailsQuery, MovieDetails>
{
    public async Task<MovieDetails> HandleAsync(GetMovieDetailsQuery query, CancellationToken cancellationToken)
    {
        var details = await catalog.GetDetailsAsync(query.Id, cancellationToken);
        return details ?? throw new NotFoundException();
    }
}
