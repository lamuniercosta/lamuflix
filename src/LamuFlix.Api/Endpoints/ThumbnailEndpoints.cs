using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace LamuFlix.Api.Endpoints;

internal static class ThumbnailEndpoints
{
    private const string JpegContentType = "image/jpeg";
    private const string ContentTypeOptionsHeader = "X-Content-Type-Options";
    private const string NoSniffValue = "nosniff";

    internal static RouteGroupBuilder MapThumbnailEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/movies/{id:int}/thumbnail", GetThumbnailAsync)
            .WithName("GetMovieThumbnail")
            .WithSummary("Stream the cached thumbnail JPEG for a movie.")
            .Produces<byte[]>(contentType: JpegContentType)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<IResult> GetThumbnailAsync(
        int id,
        IOptions<ThumbnailOptions> options,
        ThumbnailCache cache,
        IQueryHandler<GetMovieDetailsQuery, MovieDetails> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled || !MovieId.TryCreate(id, out var movieId))
        {
            throw new NotFoundException();
        }

        await handler.HandleAsync(new GetMovieDetailsQuery(movieId), cancellationToken);

        if (!cache.TryOpenCanonical(movieId, out var stream) || stream is null)
        {
            throw new NotFoundException();
        }

        httpContext.Response.RegisterForDispose(stream);
        httpContext.Response.Headers[ContentTypeOptionsHeader] = NoSniffValue;
        return TypedResults.Stream(stream, JpegContentType);
    }
}
