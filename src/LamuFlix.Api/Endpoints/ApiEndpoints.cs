using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace LamuFlix.Api.Endpoints;

internal static class ApiEndpoints
{
    internal static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");

        return api.MapLibraryEndpoints().MapImportEndpoints().MapEnrichmentEndpoints().MapWatchlistEndpoints();
    }
}
