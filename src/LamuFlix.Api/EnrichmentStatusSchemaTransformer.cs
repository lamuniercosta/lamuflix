using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LamuFlix.Api;

internal sealed class EnrichmentStatusSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (context.JsonTypeInfo.Type != typeof(EnrichmentStatus))
        {
            return Task.CompletedTask;
        }

        schema.Type = JsonSchemaType.String;
        schema.Enum =
        [
            JsonValue.Create(nameof(EnrichmentStatus.Pending)),
            JsonValue.Create(nameof(EnrichmentStatus.Enriched)),
            JsonValue.Create(nameof(EnrichmentStatus.NotFound)),
            JsonValue.Create(nameof(EnrichmentStatus.Failed)),
        ];
        return Task.CompletedTask;
    }
}
