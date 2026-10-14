using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OpenApi;

namespace LamuFlix.Api;

internal static class OpenApiDocumentNormalizer
{
    private const string ApiRoutePrefix = "/api";

    public static Task NormalizeAsync(OpenApiDocument document)
    {
        foreach (var path in document.Paths.Keys.Where(static path => !IsApiBusinessPath(path)).ToList())
        {
            document.Paths.Remove(path);
        }

        if (document.Components?.Schemas is { } schemas
            && schemas.TryGetValue("EnrichmentStatus", out var schema)
            && schema is OpenApiSchema concrete)
        {
            concrete.Type = JsonSchemaType.String;
            concrete.Enum =
            [
                System.Text.Json.Nodes.JsonValue.Create("Pending"),
                System.Text.Json.Nodes.JsonValue.Create("Enriched"),
                System.Text.Json.Nodes.JsonValue.Create("NotFound"),
                System.Text.Json.Nodes.JsonValue.Create("Failed"),
            ];
        }

        return Task.CompletedTask;
    }

    private static bool IsApiBusinessPath(string path) =>
        string.Equals(path, ApiRoutePrefix, StringComparison.Ordinal)
        || path.StartsWith(ApiRoutePrefix + "/", StringComparison.Ordinal);
}
