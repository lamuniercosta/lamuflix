using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LamuFlix.Api;

internal sealed class RecommendationsOperationTransformer : IOpenApiOperationTransformer
{
    private const string GetRecommendationsOperationId = "GetRecommendations";
    private const string TakeParameterName = "take";
    private const string TakeDescription =
        "The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        if (!string.Equals(operation.OperationId, GetRecommendationsOperationId, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        operation.Parameters ??= [];

        if (ContainsTakeParameter(operation.Parameters))
        {
            return Task.CompletedTask;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = TakeParameterName,
            In = ParameterLocation.Query,
            Required = false,
            Description = TakeDescription,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Default = JsonValue.Create(10),
                Minimum = "1",
            },
        });

        return Task.CompletedTask;
    }

    private static bool ContainsTakeParameter(IList<IOpenApiParameter> parameters) =>
        parameters.OfType<OpenApiParameter>().Any(static parameter =>
            string.Equals(parameter.Name, TakeParameterName, StringComparison.Ordinal)
            && parameter.In == ParameterLocation.Query);
}
