using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LamuFlix.Api;

internal sealed class ProblemDetailsSchemaTransformer : IOpenApiSchemaTransformer
{
    private const string ReasonPropertyName = "reason";
    private const string TraceIdPropertyName = "traceId";

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (context.JsonTypeInfo.Type != typeof(ProblemDetails))
        {
            return Task.CompletedTask;
        }

        schema.Properties ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        schema.Properties[ReasonPropertyName] = new OpenApiSchema { Type = JsonSchemaType.String };
        schema.Properties[TraceIdPropertyName] = new OpenApiSchema { Type = JsonSchemaType.String };
        return Task.CompletedTask;
    }
}
