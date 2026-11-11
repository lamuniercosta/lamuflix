using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LamuFlix.Api.Endpoints;

internal static class BulkImportEndpoints
{
    internal static RouteGroupBuilder MapBulkImportEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/movies/import/candidates", ListCandidatesAsync)
            .WithName("ListImportCandidates")
            .WithSummary("List library folders not yet imported.")
            .Produces<ImportCandidatesResponse>()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        api.MapPost("/movies/import/library", ImportLibraryAsync)
            .WithName("ImportLibraryFolders")
            .WithSummary("Import selected library folders and retry failed enrichments.")
            .Produces<ImportLibraryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<Ok<ImportCandidatesResponse>> ListCandidatesAsync(
        IQueryHandler<ImportCandidatesQuery, ImportCandidatesResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ImportCandidatesQuery(), cancellationToken);
        return TypedResults.Ok(ImportCandidatesResponse.From(result));
    }

    private static async Task<Ok<ImportLibraryResponse>> ImportLibraryAsync(
        ImportLibraryRequest request,
        ICommandHandler<ImportLibraryFoldersCommand, ImportLibraryFoldersResult> handler,
        CancellationToken cancellationToken)
    {
        var folders = request.Folders ?? [];
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand([.. folders], request.RetryFailed), cancellationToken);
        return TypedResults.Ok(ImportLibraryResponse.From(result));
    }
}

internal sealed record ImportLibraryRequest(string[]? Folders, bool RetryFailed);
