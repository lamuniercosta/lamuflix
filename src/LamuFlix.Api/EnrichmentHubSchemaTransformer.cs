using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LamuFlix.Api;

internal sealed class EnrichmentHubSchemaTransformer : IOpenApiDocumentTransformer
{
    private const string SchemaId = nameof(EnrichmentStatusChangedDto);
    private const string StatusSchemaReference = "#/components/schemas/" + nameof(EnrichmentStatus);
    private const string IntegerFormat = "int32";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        document.Components.Schemas[SchemaId] = CreateSchema(document);
        return Task.CompletedTask;
    }

    private static OpenApiSchema CreateSchema(OpenApiDocument document)
    {
        var constructor = typeof(EnrichmentStatusChangedDto)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();
        var properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        var required = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parameter in constructor.GetParameters())
        {
            var wireName = ToWireName(parameter);
            properties[wireName] = CreatePropertySchema(document, parameter.ParameterType, wireName);
            required.Add(wireName);
        }

        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Required = required,
            Properties = properties,
        };
    }

    private static IOpenApiSchema CreatePropertySchema(OpenApiDocument document, Type type, string wireName)
    {
        if (type == typeof(int))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Integer, Format = IntegerFormat };
        }

        if (type == typeof(EnrichmentStatus))
        {
            return new OpenApiSchemaReference(StatusSchemaReference, document, externalResource: null);
        }

        throw new NotSupportedException(
            $"EnrichmentStatusChangedDto.{wireName} has unsupported OpenAPI schema type '{type}'.");
    }

    private static string ToWireName(ParameterInfo parameter)
    {
        var name = parameter.Name
            ?? throw new InvalidOperationException(
                $"EnrichmentStatusChangedDto constructor parameter {parameter.Position} has no name.");
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
