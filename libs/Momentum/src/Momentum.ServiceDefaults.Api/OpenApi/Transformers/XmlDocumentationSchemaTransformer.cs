// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Momentum.Extensions.XmlDocs;
using Momentum.ServiceDefaults.Api.OpenApi.Extensions;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Momentum.ServiceDefaults.Api.OpenApi.Transformers;

/// <summary>
///     Transforms OpenAPI schemas by enriching them with XML documentation from model types.
/// </summary>
/// <remarks>
///     This transformer enhances schema specifications with:
///     <list type="bullet">
///         <item>Type XML documentation as schema description</item>
///         <item>Property documentation for all public properties</item>
///         <item>Nullable type information</item>
///         <item>Enum value listings</item>
///         <item>Support for JsonPropertyName attributes</item>
///     </list>
/// </remarks>
public class XmlDocumentationSchemaTransformer(
    ILogger<XmlDocumentationSchemaTransformer> logger,
    IXmlDocumentationService xmlDocumentationService
) : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        try
        {
            EnrichSchema(schema, context.JsonTypeInfo.Type);
            EnrichProperties(schema, context.JsonTypeInfo.Type);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to transform schema with XML documentation for type {TypeName}", context.JsonTypeInfo.Type.Name);
        }

        return Task.CompletedTask;
    }

    private void EnrichSchema(OpenApiSchema schema, Type type)
    {
        var typeDocs = xmlDocumentationService.GetTypeDocumentation(type);

        if (typeDocs is null)
            return;

        schema.EnrichWithXmlDocInfo(typeDocs, type);
    }

    private void EnrichProperties(OpenApiSchema schema, Type type)
    {
        if (schema.Properties is null || schema.Properties.Count == 0)
            return;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertyLookup = properties.ToDictionary(GetJsonPropertyName, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var (propertyName, propertySchema) in schema.Properties)
        {
            if (propertyLookup.TryGetValue(propertyName, out var propertyInfo))
            {
                EnrichPropertySchema(propertySchema, propertyInfo);
            }
        }
    }

    private void EnrichPropertySchema(IOpenApiSchema propertySchema, PropertyInfo propertyInfo)
    {
        var propDoc = xmlDocumentationService.GetPropertyDocumentation(propertyInfo);

        if (propDoc is not null && propertySchema is OpenApiSchema openApiSchema)
        {
            openApiSchema.EnrichWithXmlDocInfo(propDoc, propertyInfo.PropertyType);
        }

        AddTypeSpecificInformation(propertySchema, propertyInfo);
    }

    private static void AddTypeSpecificInformation(IOpenApiSchema propertySchema, PropertyInfo propertyInfo)
    {
        if (propertySchema is not OpenApiSchema openApiSchema)
            return;

        var propertyType = propertyInfo.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(propertyType);

        // Note: OpenApiSchema.Nullable was removed in OpenApi v2
        // Nullable types are now represented via the Type property using JsonSchemaType flags
        if (underlyingType is not null)
        {
            propertyType = underlyingType;
            // Mark as nullable using the type system
            openApiSchema.Type |= JsonSchemaType.Null;
        }

        if (propertyType.IsEnum)
        {
            var enumValues = Enum.GetNames(propertyType);

            if (enumValues.Length > 0)
            {
                openApiSchema.Enum = enumValues
                    .Select(e => JsonValue.Create(e)!)
                    .Cast<JsonNode>()
                    .ToList();
            }
        }
    }

    private static string GetJsonPropertyName(PropertyInfo propertyInfo)
    {
        var jsonPropertyAttr = propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>();

        if (jsonPropertyAttr is not null)
            return jsonPropertyAttr.Name;

        return char.ToLowerInvariant(propertyInfo.Name[0]) + propertyInfo.Name[1..];
    }
}
