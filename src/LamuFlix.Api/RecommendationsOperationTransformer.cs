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
    private const string GetForYouRecommendationsOperationId = "GetForYouRecommendations";
    private const string TakeParameterName = "take";
    private const string OffsetParameterName = "offset";
    private const string ExcludeParameterName = "exclude";
    private const string TakeDescription =
        "The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        if (!IsRecommendationsOperation(operation.OperationId))
        {
            return Task.CompletedTask;
        }

        operation.Parameters ??= [];

        if (!ContainsTakeParameter(operation.Parameters))
        {
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
        }

        if (string.Equals(operation.OperationId, GetForYouRecommendationsOperationId, StringComparison.Ordinal))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = OffsetParameterName,
                In = ParameterLocation.Query,
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Integer,
                    Default = JsonValue.Create(0),
                    Minimum = "0",
                },
            });
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = ExcludeParameterName,
                In = ParameterLocation.Query,
                Required = false,
                Style = ParameterStyle.Form,
                Explode = true,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = new OpenApiSchema { Type = JsonSchemaType.Integer },
                },
            });
        }

        return Task.CompletedTask;
    }

    private static bool IsRecommendationsOperation(string? operationId) =>
        string.Equals(operationId, GetRecommendationsOperationId, StringComparison.Ordinal)
        || string.Equals(operationId, GetForYouRecommendationsOperationId, StringComparison.Ordinal);

    private static bool ContainsTakeParameter(IList<IOpenApiParameter> parameters) =>
        parameters.OfType<OpenApiParameter>().Any(static parameter =>
            string.Equals(parameter.Name, TakeParameterName, StringComparison.Ordinal)
            && parameter.In == ParameterLocation.Query);
}
