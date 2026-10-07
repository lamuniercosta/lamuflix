using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LamuFlix.Api.Endpoints;

internal static class ImportEndpoints
{
    internal static RouteGroupBuilder MapImportEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/movies/import", ImportMovieAsync)
            .WithName("ImportMovie")
            .WithSummary("Import a movie folder into the library.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<Accepted> ImportMovieAsync(
        ImportMovieRequest request,
        ICommandHandler<ImportMovieFolderCommand, MovieId> handler,
        CancellationToken cancellationToken)
    {
        if (!LibraryPath.TryCreate(request.FolderPath, out var folder))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["folderPath"] = ["Folder path must be non-blank and must not contain '..'."],
            });
        }

        var id = await handler.HandleAsync(new ImportMovieFolderCommand(folder), cancellationToken);
        return TypedResults.Accepted($"/api/movies/{id.Value}");
    }
}
