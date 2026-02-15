// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Extensions;
using System.Reflection;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.EventMarkdownGenerator.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

public static class AssemblyEventDiscovery
{
    private const string EventTopicAttributeName = nameof(EventTopicAttribute);

    public static IEnumerable<EventMetadata> DiscoverEvents(Assembly assembly, XmlDocumentationParser? xmlParser,
        PayloadSizeCalculator calculator)
    {
        var defaultDomain = GetMainDomainName(assembly);
        var integrationEventTypes = GetEventTypes(assembly);

        return integrationEventTypes.Select(type => CreateEventMetadata(type, defaultDomain, xmlParser, calculator));
    }

    private static IEnumerable<Type> GetEventTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes().Where(IsEventType);
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Handle missing dependencies gracefully - return only the types that loaded successfully
            var loadedTypes = ex.Types.Where(t => t != null).Cast<Type>();

            return loadedTypes.Where(IsEventType);
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or TypeLoadException)
        {
            // Return empty collection if we can't load any types due to missing dependencies
            return [];
        }
    }

    private static bool IsEventType(Type type)
    {
        // Check for EventTopicAttribute by name only, to work across assembly load contexts
        // Return true if it has the attribute, regardless of namespace
        return type.GetCustomAttributes()
            .Any(attr => attr.GetType().Name.StartsWith(EventTopicAttributeName));
    }

    private static EventMetadata CreateEventMetadata(Type eventType, string defaultDomain,
        XmlDocumentationParser? xmlParser, PayloadSizeCalculator calculator)
    {
        // Use dynamic attribute handling to work across assembly contexts
        var topicAttribute = GetEventTopicAttributeDynamic(eventType);
        var obsoleteAttribute = eventType.GetCustomAttribute<ObsoleteAttribute>();
        var (properties, partitionKeys) = GetEventPropertiesAndPartitionKeys(eventType, xmlParser, calculator);

        var topicName = GetTopicName(topicAttribute, eventType);

        // Access properties via reflection for cross-assembly compatibility
        var (shouldPluralize, domain, isInternal, version) = GetTopicAttributeProperties(topicAttribute);

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
            EventName = eventType.Name,
            FullTypeName = eventType.FullName ?? eventType.Name,
            Namespace = eventType.Namespace ?? string.Empty,
            TopicName = fullTopicName,
            Domain = eventDomain,
            Version = version,
            IsInternal = isInternal,
            EventType = eventType,
            TopicAttribute = GetEventTopicAttribute<Attribute>(eventType),
            Properties = properties,
            PartitionKeys = partitionKeys,
            ObsoleteMessage = obsoleteAttribute?.Message
        };
    }

    private static Attribute GetEventTopicAttributeDynamic(Type type)
    {
        // Find attribute by name to work across assembly load contexts
        var foundAttribute = type.GetCustomAttributes()
            .FirstOrDefault(attr => attr.GetType().Name.StartsWith(EventTopicAttributeName));

        if (foundAttribute == null)
        {
            throw new InvalidOperationException($"EventTopicAttribute not found on type {type.Name}");
        }

        return foundAttribute;
    }

    private static T GetEventTopicAttribute<T>(Type type) where T : Attribute
    {
        var attribute = type.GetCustomAttributes<T>().FirstOrDefault();

        if (attribute is not null)
        {
            return attribute;
        }

        var foundAttribute = type.GetCustomAttributes()
            .FirstOrDefault(attr => attr.GetType().Name.StartsWith(EventTopicAttributeName));

        if (foundAttribute is T typedAttribute)
        {
            return typedAttribute;
        }

        // If T is an attribute (base type), return any EventTopicAttribute found
        if (typeof(T) == typeof(Attribute) && foundAttribute != null)
        {
            return (T)foundAttribute;
        }

        throw new InvalidOperationException($"EventTopicAttribute not found on type {type.Name}");
    }

    private static (List<EventPropertyMetadata> properties, List<PartitionKeyMetadata> partitionKeys) GetEventPropertiesAndPartitionKeys(
        Type eventType, XmlDocumentationParser? xmlParser, PayloadSizeCalculator calculator)
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

            // Check for PartitionKey attribute on the property
            var partitionKeyAttr = property.GetCustomAttribute<PartitionKeyAttribute>();
            var isPartitionKey = partitionKeyAttr != null;

            // If not found on property, check corresponding constructor parameter for records
            if (!isPartitionKey && parameterToPropertyMap.TryGetValue(property.Name, out var parameter))
            {
                partitionKeyAttr = parameter.GetCustomAttribute<PartitionKeyAttribute>();
                isPartitionKey = partitionKeyAttr != null;
            }

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
                PartitionKeyOrder = partitionKeyAttr?.Order,
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
                    Order = partitionKeyAttr!.Order,
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

    private static string GetMainDomainName(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name ?? "Unknown";
        var assemblyParts = assemblyName.Split('.');

        return assemblyParts[0];
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
    private static (bool shouldPluralize, string? domain, bool isInternal, string version) GetTopicAttributeProperties(
        object topicAttribute)
    {
        var attrType = topicAttribute.GetType();

        var shouldPluralize = GetPropertyValue<bool?>(attrType, topicAttribute, "ShouldPluralizeTopicName") ?? false;
        var domain = GetPropertyValue<string?>(attrType, topicAttribute, "Domain");
        var isInternal = GetPropertyValue<bool?>(attrType, topicAttribute, "Internal") ?? false;
        var version = GetPropertyValue<string?>(attrType, topicAttribute, "Version") ?? "v1";

        return (shouldPluralize, domain, isInternal, version);
    }

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
