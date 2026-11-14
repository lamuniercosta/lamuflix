using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace LamuFlix.Api.Endpoints;

internal static class ImportEndpoints
{
    private const string FolderPathKey = "folderPath";
    private const string MalformedFolderMessage = "Folder path must be non-blank and must not contain '..'.";
    private const string LibraryRootMessage =
        "Folder path must name a movie folder inside the library root, not the library root itself.";

    internal static RouteGroupBuilder MapImportEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/movies/import", ImportMovieAsync)
            .WithName("ImportMovie")
            .WithSummary("Import a movie folder into the library.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<Accepted> ImportMovieAsync(
        ImportMovieRequest request,
        ICommandHandler<ImportMovieFolderCommand, MovieId> handler,
        IOptions<LibraryOptions> libraryOptions,
        CancellationToken cancellationToken)
    {
        var folder = RequireImportableFolder(request.FolderPath, libraryOptions.Value.RootPath);
        var id = await handler.HandleAsync(new ImportMovieFolderCommand(folder), cancellationToken);
        return TypedResults.Accepted($"/api/movies/{id.Value}");
    }

    private static LibraryPath RequireImportableFolder(string? folderPath, string rootPath)
    {
        if (!LibraryPath.TryCreate(folderPath, out var folder))
        {
            throw FieldError(MalformedFolderMessage);
        }

        if (LibraryPath.ResolvesToLibraryRoot(folderPath, rootPath))
        {
            throw FieldError(LibraryRootMessage);
        }

        return folder;
    }

    private static ValidationException FieldError(string message) =>
        new(new Dictionary<string, string[]>
        {
            [FolderPathKey] = [message],
        });
}
