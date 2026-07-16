// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;
using Momentum.Extensions.Abstractions.Extensions;
using Momentum.Extensions.EventMarkdownGenerator.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

/// <summary>
///     Builds <see cref="EventMetadata"/> for a single discovered event type. Separated from
///     <see cref="AssemblyEventDiscovery"/> so assembly/type scanning stays independent of the
///     (considerably larger) job of reflecting an event type and its topic attribute into metadata.
/// </summary>
public static class EventMetadataBuilder
{
    public static EventMetadata Build(Type eventType, string defaultDomain, XmlDocumentationParser? xmlParser,
        PayloadSizeCalculator calculator, string attributeNamePrefix, string partitionKeyAttributeNamePrefix)
    {
        // Use dynamic attribute handling to work across assembly contexts
        var topicAttribute = GetEventTopicAttributeDynamic(eventType, attributeNamePrefix);
        var obsoleteAttribute = eventType.GetCustomAttribute<ObsoleteAttribute>();
        var (properties, partitionKeys) =
            GetEventPropertiesAndPartitionKeys(eventType, xmlParser, calculator, partitionKeyAttributeNamePrefix);

        var topicName = GetTopicName(topicAttribute, eventType);

        // Access properties via reflection for cross-assembly compatibility
        var (shouldPluralize, domain, isInternal, version, eventNameOverride) = GetTopicAttributeProperties(topicAttribute);

        // Simple pluralization fallback - add 's' to the end
        // This is a fallback when the extension method is not available
        if (shouldPluralize && !topicName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            topicName = topicName.Pluralize();
        }

        var eventDomain = !string.IsNullOrWhiteSpace(domain)
            ? domain
            : GetDomainFromNamespace(eventType.Namespace) ?? defaultDomain;

        // Build full topic name: {env}.{domain}.{visibility}.{topic}.{version}
        var visibility = isInternal ? "internal" : "public";

        var fullTopicName = $"{{env}}.{defaultDomain.ToLowerInvariant()}.{visibility}.{topicName}.{version}";

        return new EventMetadata
        {
            EventName = !string.IsNullOrWhiteSpace(eventNameOverride) ? eventNameOverride : eventType.Name,
            EventTypeName = eventType.Name,
            FullTypeName = eventType.FullName ?? eventType.Name,
            Namespace = eventType.Namespace ?? string.Empty,
            Topic = topicName,
            FullyQualifiedTopicName = fullTopicName,
            Domain = eventDomain,
            Version = version,
            IsInternal = isInternal,
            EventType = eventType,
            TopicAttribute = topicAttribute,
            AttributeProperties = GetAttributeProperties(topicAttribute),
            Properties = properties,
            PartitionKeys = partitionKeys,
            ObsoleteMessage = obsoleteAttribute?.Message
        };
    }

    /// <summary>
    ///     Reflects every public instance property of the topic attribute into a name/value dictionary,
    ///     skipping members inherited from <see cref="Attribute"/> itself (e.g. TypeId). Uses all public
    ///     instance properties rather than DeclaredOnly, since generic attribute subtypes (e.g.
    ///     EventTopicAttribute&lt;TEntity&gt;) typically declare no members of their own — everything lives
    ///     on the non-generic base type.
    /// </summary>
    private static Dictionary<string, string> GetAttributeProperties(Attribute topicAttribute)
    {
        var attrType = topicAttribute.GetType();
        var result = new Dictionary<string, string>();

        foreach (var property in attrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.DeclaringType == typeof(Attribute) || property.GetIndexParameters().Length > 0)
                continue;

            var value = property.GetValue(topicAttribute);
            result[property.Name] = value?.ToString() ?? string.Empty;
        }

        return result;
    }

    private static Attribute GetEventTopicAttributeDynamic(Type type, string attributeNamePrefix)
    {
        var foundAttribute = type.GetCustomAttributes()
            .FirstOrDefault(attr => attr.GetType().Name.StartsWith(attributeNamePrefix));

        if (foundAttribute == null)
        {
            throw new InvalidOperationException($"Attribute with prefix '{attributeNamePrefix}' not found on type {type.Name}");
        }

        return foundAttribute;
    }

    private static (List<EventPropertyMetadata> properties, List<PartitionKeyMetadata> partitionKeys) GetEventPropertiesAndPartitionKeys(
        Type eventType, XmlDocumentationParser? xmlParser, PayloadSizeCalculator calculator, string partitionKeyAttributeNamePrefix)
    {
        var properties = new List<EventPropertyMetadata>();
        var partitionKeys = new List<PartitionKeyMetadata>();

        var constructor = eventType.GetConstructors().FirstOrDefault();
        var constructorParameters = constructor?.GetParameters() ?? [];

        var parameterToPropertyMap = MapConstructorParametersToProperties(eventType, constructorParameters);

        var eventDoc = xmlParser?.GetEventDocumentation(eventType);

        foreach (var property in eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var isComplexType = !TypeUtils.IsPrimitiveType(property.PropertyType);
            var isRequired = TypeUtils.IsRequiredProperty(property);

            // Check for the partition key attribute by name (generic, works across assembly load contexts)
            var partitionKeyAttr = FindAttributeByName(property.GetCustomAttributes(), partitionKeyAttributeNamePrefix);
            var isPartitionKey = partitionKeyAttr != null;

            // If not found on property, check corresponding constructor parameter for records
            if (!isPartitionKey && parameterToPropertyMap.TryGetValue(property.Name, out var parameter))
            {
                partitionKeyAttr = FindAttributeByName(parameter.GetCustomAttributes(), partitionKeyAttributeNamePrefix);
                isPartitionKey = partitionKeyAttr != null;
            }

            var partitionKeyOrder = partitionKeyAttr is null
                ? (int?)null
                : GetPropertyValue<int?>(partitionKeyAttr.GetType(), partitionKeyAttr, "Order") ?? 0;

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

    private static Dictionary<string, ParameterInfo> MapConstructorParametersToProperties(Type eventType,
        ParameterInfo[] constructorParameters)
    {
        var map = new Dictionary<string, ParameterInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in constructorParameters)
        {
            var property = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));

            if (property != null)
            {
                map[property.Name] = parameter;
            }
        }

        return map;
    }

    private static string? GetDomainFromNamespace(string? namespaceName)
    {
        if (string.IsNullOrEmpty(namespaceName))
            return null;

        // For namespaces like "AppDomain.Cashiers.Contracts.IntegrationEvents", extract "Cashiers"
        // The pattern is: Domain.Subdomain.[SomeNameSpace].Contracts.IntegrationEvents
        var parts = namespaceName.Split('.');

        var contractsIndex = Array.LastIndexOf(parts, "Contracts");

        if (contractsIndex > 0)
        {
            return parts[contractsIndex - 1];
        }

        return parts[0];
    }

    /// <summary>
    ///     Simple kebab case conversion when extension method is not available
    /// </summary>
    private static string ConvertToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = string.Concat(
            input.Select((x, i) => i > 0 && char.IsUpper(x)
                ? "-" + char.ToLower(x)
                : char.ToLower(x).ToString())
        );

        return result;
    }

    /// <summary>
    ///     Extracts properties from EventTopicAttribute using reflection for cross-assembly compatibility.
    /// </summary>
    private static (bool shouldPluralize, string? domain, bool isInternal, string version, string? eventName)
        GetTopicAttributeProperties(object topicAttribute)
    {
        var attrType = topicAttribute.GetType();

        var shouldPluralize = GetPropertyValue<bool?>(attrType, topicAttribute, "ShouldPluralizeTopicName") ?? false;
        var domain = GetPropertyValue<string?>(attrType, topicAttribute, "Domain");
        var isInternal = GetPropertyValue<bool?>(attrType, topicAttribute, "Internal") ?? false;
        var version = GetPropertyValue<string?>(attrType, topicAttribute, "Version") ?? "v1";
        var eventName = GetPropertyValue<string?>(attrType, topicAttribute, "EventName");

        return (shouldPluralize, domain, isInternal, version, eventName);
    }

    /// <summary>
    ///     Finds a custom attribute by name (or name prefix) so discovery works across assembly load contexts
    ///     and for any attribute type matching the configured convention, not just a hardcoded compiled type.
    /// </summary>
    private static Attribute? FindAttributeByName(IEnumerable<Attribute> attributes, string attributeNamePrefix) =>
        attributes.FirstOrDefault(attr => attr.GetType().Name.StartsWith(attributeNamePrefix));

    /// <summary>
    ///     Gets a property value from an object using reflection with safe null handling.
    /// </summary>
    private static T? GetPropertyValue<T>(Type type, object instance, string propertyName)
    {
        var property = type.GetProperty(propertyName);

        if (property == null)
            return default;

        var value = property.GetValue(instance);

        return value is T typedValue ? typedValue : default;
    }

    /// <summary>
    ///     Gets the topic name from either generic or string-based EventTopicAttribute
    /// </summary>
    private static string GetTopicName(object topicAttribute, Type eventType)
    {
        var attrType = topicAttribute.GetType();
        var topic = GetPropertyValue<string?>(attrType, topicAttribute, "Topic");

        if (!string.IsNullOrEmpty(topic))
            return topic;

        // Fallback kebab case conversion when extension method is not available
        return ConvertToKebabCase(eventType.Name);
    }
}
