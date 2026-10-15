using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LamuFlix.Api;

internal sealed class BrowseMoviesOperationTransformer : IOpenApiOperationTransformer
{
    private const string BrowseMoviesOperationId = "BrowseMovies";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        if (!string.Equals(operation.OperationId, BrowseMoviesOperationId, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        Apply("Sort", "sort", new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum =
            [
                JsonValue.Create("Title"),
                JsonValue.Create("Year"),
                JsonValue.Create("Rating"),
                JsonValue.Create("Runtime"),
            ],
        });
        Apply("Direction", "direction", new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum =
            [
                JsonValue.Create("Ascending"),
                JsonValue.Create("Descending"),
            ],
        });
        Apply("Page", "page", IntegerBounds("1", null));
        Apply("PageSize", "pageSize", IntegerBounds("1", "100"));
        return Task.CompletedTask;

        void Apply(string generatedName, string wireName, OpenApiSchema schema)
        {
            if (operation.Parameters is null)
            {
                return;
            }

            for (var index = 0; index < operation.Parameters.Count; index++)
            {
                if (operation.Parameters[index] is not OpenApiParameter parameter
                    || !string.Equals(parameter.Name, generatedName, StringComparison.Ordinal))
                {
                    continue;
                }

                parameter.Name = wireName;
                parameter.Required = true;
                parameter.Schema = schema;
                operation.Parameters[index] = parameter;
                return;
            }
        }
    }

    private static OpenApiSchema IntegerBounds(string minimum, string? maximum) =>
        new()
        {
            Type = JsonSchemaType.Integer,
            Minimum = minimum,
            Maximum = maximum,
        };
}
