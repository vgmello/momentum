// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;
using System.Text.RegularExpressions;
using Momentum.Extensions.EventMarkdownGenerator.Models;
using Momentum.Extensions.EventMarkdownGenerator.Templates.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

/// <summary>
///     Factory for creating view models used in Liquid template rendering.
///     Extracts complex object creation logic from FluidMarkdownGenerator.
/// </summary>
public static partial class EventViewModelFactory
{
    /// <summary>
    ///     Creates an event view model for template rendering.
    /// </summary>
    /// <param name="metadata">The event metadata to render.</param>
    /// <param name="documentation">The event's XML documentation.</param>
    /// <param name="options">Optional generator options for customization.</param>
    /// <param name="schemaTypes">
    ///     Types with generated schema documentation, used to link <see cref="EventViewModel.Entity"/> to its
    ///     schema page when <see cref="EventMetadata.EntityType"/> is among them.
    /// </param>
    public static EventViewModel CreateEventModel(EventMetadata metadata, EventDocumentation documentation,
        GeneratorOptions? options = null, IReadOnlySet<Type>? schemaTypes = null)
    {
        var totalSize = metadata.Properties.Sum(p => p.EstimatedSizeBytes);
        var hasInaccurateEstimates = metadata.Properties.Any(p => !p.IsAccurate);

        var entitySchemaLink = metadata.EntityType != null && schemaTypes?.Contains(metadata.EntityType) == true
            ? GetSchemaFileName(metadata.EntityType)
            : null;

        return new EventViewModel
        {
            EventName = metadata.EventName,
            EventNameKebab = metadata.EventNameKebab,
            EventTypeName = metadata.EventTypeName,
            FullTypeName = metadata.FullTypeName,
            Namespace = metadata.Namespace,
            Topic = metadata.Topic,
            FullyQualifiedTopicName = metadata.FullyQualifiedTopicName,
            Domain = metadata.Domain,
            Subdomain = metadata.Subdomain,
            Version = metadata.Version,
            Status = metadata.GetStatus(),
            Entity = metadata.Entity,
            EntitySchemaLink = entitySchemaLink,
            IsObsolete = metadata.IsObsolete,
            ObsoleteMessage = metadata.ObsoleteMessage,
            IsInternal = metadata.IsInternal,
            GithubUrl = GenerateGitHubUrl(metadata, options?.GitHubBaseUrl),
            TopicAttributeDisplayName = GetTopicAttributeDisplayName(metadata.TopicAttribute),
            AttributeProperties = metadata.AttributeProperties
                .Select(kvp => new AttributePropertyViewModel { Key = kvp.Key, Value = kvp.Value })
                .ToArray(),
            Description = documentation.GetDescription(),
            Summary = documentation.Summary,
            Remarks = documentation.Remarks,
            Example = documentation.Example,
            Properties = metadata.Properties.Select(p => new EventPropertyViewModel
            {
                Name = p.Name,
                TypeName = p.TypeName,
                IsRequired = p.IsRequired,
                IsComplexType = p.IsComplexType,
                IsCollectionType = TypeUtils.IsCollectionType(p.PropertyType),
                Description = GetPropertyDescription(p),
                SchemaLink = GetSchemaPath(p.PropertyType),
                SchemaPath = GetSchemaPath(p.PropertyType),
                ElementTypeName = TypeUtils.IsCollectionType(p.PropertyType) ? TypeUtils.GetElementTypeName(p.PropertyType) : null,
                ElementSchemaPath = GetSchemaPath(p.PropertyType),
                EstimatedSizeBytes = p.EstimatedSizeBytes,
                IsAccurate = p.IsAccurate,
                SizeWarning = p.SizeWarning
            }).ToArray(),
            PartitionKeys = metadata.PartitionKeys.Select(pk => new PartitionKeyViewModel
            {
                Name = pk.Name,
                TypeName = pk.TypeName,
                Description = GetPartitionKeyDescription(pk),
                Order = pk.Order
            }).ToArray(),
            TotalEstimatedSizeBytes = totalSize,
            HasInaccurateEstimates = hasInaccurateEstimates
        };
    }

    /// <summary>
    ///     Creates a schema model for template rendering.
    /// </summary>
    public static object CreateSchemaModel(Type schemaType)
    {
        var properties = schemaType.GetProperties()
            .Where(p => p is { CanRead: true, GetMethod.IsPublic: true })
            .ToList();

        return new
        {
            name = schemaType.Name,
            fullName = schemaType.FullName,
            description = GetTypeDescription(schemaType),
            properties = properties.Select(p => new
            {
                name = p.Name,
                typeName = GetTypeDisplayName(p.PropertyType),
                isRequired = TypeUtils.IsRequiredProperty(p),
                isComplexType = TypeUtils.IsComplexType(p.PropertyType),
                isCollectionType = TypeUtils.IsCollectionType(p.PropertyType),
                description = GetPropertyDescription(p),
                schemaLink = GetSchemaPath(p.PropertyType),
                schemaPath = GetSchemaPath(p.PropertyType)
            }).ToArray()
        };
    }

    private static string GenerateGitHubUrl(EventMetadata metadata, string? gitHubBaseUrl)
    {
        if (string.IsNullOrEmpty(gitHubBaseUrl))
        {
            return "#";
        }

        var pathParts = metadata.Namespace.Split('.');
        var filePath = string.Join("/", pathParts) + $"/{metadata.EventTypeName}.cs";

        return $"{gitHubBaseUrl}/{filePath}";
    }

    private static string GetTopicAttributeDisplayName(Attribute topicAttribute)
    {
        var attrType = topicAttribute.GetType();
        var name = "EventTopic";

        if (attrType.IsGenericType)
        {
            var genericArgs = attrType.GetGenericArguments();

            if (genericArgs.Length > 0)
            {
                var genericArg = genericArgs[0];
                name = $"EventTopic<{genericArg.Name}>";
            }
        }

        return $"[{name}]";
    }

    private static string GetPropertyDescription(EventPropertyMetadata property)
    {
        var description = property.Description ?? "No description available";

        if (property.IsComplexType && !TypeUtils.IsCollectionType(property.PropertyType) &&
            description.StartsWith(property.TypeName, StringComparison.OrdinalIgnoreCase))
        {
            var remainingDescription = description[property.TypeName.Length..].TrimStart();
            description = $"Complete {property.TypeName.ToLowerInvariant()} {remainingDescription}";
        }

        if (property.IsPartitionKey)
        {
            description += " (partition key)";
        }

        // Descriptions are rendered inline in a markdown table cell, so multi-line XML doc summaries
        // must be flattened to a single line (collapsing newlines and their surrounding whitespace).
        return CollapseWhitespace(description);
    }

    private static string GetPropertyDescription(PropertyInfo property) => $"Gets or sets the {property.Name.ToLowerInvariant()}.";

    private static string GetSchemaFileName(Type type) => $"{type.FullName}.md";

    private static string? GetSchemaPath(Type? propertyType)
    {
        if (propertyType == null) return null;

        // For non-collection complex types, return direct schema path
        if (TypeUtils.IsComplexType(propertyType) && !TypeUtils.IsCollectionType(propertyType))
        {
            return GetSchemaFileName(propertyType);
        }

        // For collections of complex types, return the element type schema path
        if (TypeUtils.IsCollectionType(propertyType))
        {
            var elementType = TypeUtils.GetElementType(propertyType);

            if (elementType != null && TypeUtils.IsComplexType(elementType))
            {
                return GetSchemaFileName(elementType);
            }
        }

        return null;
    }

    private static string GetPartitionKeyDescription(PartitionKeyMetadata partitionKey)
    {
        return CollapseWhitespace(partitionKey.Description ?? "Used for message routing");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    /// <summary>Collapses any run of whitespace (including newlines) into a single space for inline rendering.</summary>
    private static string CollapseWhitespace(string value) => WhitespaceRegex().Replace(value, " ").Trim();

    private static string GetTypeDescription(Type type)
    {
        var typeName = type.Name.ToLowerInvariant();

        return $"Represents a {typeName} entity.";
    }

    private static string GetTypeDisplayName(Type type)
    {
        return TypeUtils.GetFriendlyTypeName(type);
    }

}
