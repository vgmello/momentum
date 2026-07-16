// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;
using Momentum.Extensions.EventMarkdownGenerator.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

/// <summary>
///     Builds an event type's <see cref="EventPropertyMetadata"/> and <see cref="PartitionKeyMetadata"/>
///     lists. Separated from <see cref="EventMetadataBuilder"/> so reflecting an event type's own
///     properties stays independent from resolving its topic attribute and computed fields (topic,
///     domain, fully-qualified topic name, etc).
/// </summary>
public static class EventPropertyMetadataBuilder
{
    public static (List<EventPropertyMetadata> Properties, List<PartitionKeyMetadata> PartitionKeys) Build(
        Type eventType, XmlDocumentationParser? xmlParser, PayloadSizeCalculator calculator, string partitionKeyAttributeNamePrefix)
    {
        var properties = new List<EventPropertyMetadata>();
        var partitionKeys = new List<PartitionKeyMetadata>();

        var constructor = eventType.GetConstructors().FirstOrDefault();
        var constructorParameters = constructor?.GetParameters() ?? [];

        var parameterToPropertyMap = TypeUtils.MapConstructorParametersToProperties(eventType, constructorParameters);

        var eventDoc = xmlParser?.GetEventDocumentation(eventType);

        foreach (var property in eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var isComplexType = !TypeUtils.IsPrimitiveType(property.PropertyType);
            var isRequired = TypeUtils.IsRequiredProperty(property);

            // Check for the partition key attribute by name (generic, works across assembly load contexts)
            var partitionKeyAttr = TypeUtils.FindAttributeByName(property.GetCustomAttributes(), partitionKeyAttributeNamePrefix);
            var isPartitionKey = partitionKeyAttr != null;

            // If not found on property, check corresponding constructor parameter for records
            if (!isPartitionKey && parameterToPropertyMap.TryGetValue(property.Name, out var parameter))
            {
                partitionKeyAttr = TypeUtils.FindAttributeByName(parameter.GetCustomAttributes(), partitionKeyAttributeNamePrefix);
                isPartitionKey = partitionKeyAttr != null;
            }

            var partitionKeyOrder = partitionKeyAttr is null
                ? (int?)null
                : TypeUtils.GetPropertyValue<int?>(partitionKeyAttr.GetType(), partitionKeyAttr, "Order") ?? 0;

            var description = eventDoc?.PropertyDescriptions?.GetValueOrDefault(property.Name) ?? "No description available";
            var sizeResult = calculator.CalculatePropertySize(property, property.PropertyType);

            properties.Add(new EventPropertyMetadata
            {
                Name = property.Name,
                TypeName = TypeUtils.GetFriendlyTypeName(property.PropertyType),
                PropertyType = property.PropertyType,
                IsRequired = isRequired,
                IsComplexType = isComplexType,
                IsPartitionKey = isPartitionKey,
                PartitionKeyOrder = partitionKeyOrder,
                Description = description,
                EstimatedSizeBytes = sizeResult.SizeBytes,
                IsAccurate = sizeResult.IsAccurate,
                SizeWarning = sizeResult.Warning
            });

            if (isPartitionKey)
            {
                partitionKeys.Add(new PartitionKeyMetadata
                {
                    Name = property.Name,
                    TypeName = TypeUtils.GetFriendlyTypeName(property.PropertyType),
                    Description = description,
                    Order = partitionKeyOrder ?? 0,
                    IsFromParameter = parameterToPropertyMap.ContainsKey(property.Name)
                });
            }
        }

        partitionKeys = partitionKeys.OrderBy(pk => pk.Order).ThenBy(pk => pk.Name).ToList();

        return (properties, partitionKeys);
    }
}
