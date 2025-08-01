// Copyright (c) ABCDEG. All rights reserved.

using Operations.Extensions.Abstractions.Extensions;
using System.Reflection;
using Operations.Extensions.Abstractions.Messaging;
using Operations.Extensions.EventMarkdownGenerator.Models;

namespace Operations.Extensions.EventMarkdownGenerator.Services;

public static class AssemblyEventDiscovery
{
    private const string IntegrationEventsNamespace = ".IntegrationEvents";
    private const string DomainEventsNamespace = ".DomainEvents";
    private const string EventTopicAttributeName = nameof(EventTopicAttribute);

    public static IEnumerable<EventMetadata> DiscoverEvents(Assembly assembly)
    {
        var defaultDomain = GetMainDomainName(assembly);
        var integrationEventTypes = GetEventTypes(assembly);

        return integrationEventTypes.Select(type => CreateEventMetadata(type, defaultDomain, null));
    }

    public static IEnumerable<EventMetadata> DiscoverEvents(Assembly assembly, XmlDocumentationParser? xmlParser)
    {
        var defaultDomain = GetMainDomainName(assembly);
        var integrationEventTypes = GetEventTypes(assembly);

        return integrationEventTypes.Select(type => CreateEventMetadata(type, defaultDomain, xmlParser));
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
        var hasEventAttribute = type.GetCustomAttribute<EventTopicAttribute>() != null ||
                                type.GetCustomAttributes().Any(attr => attr.GetType().Name.StartsWith(EventTopicAttributeName));

        var hasEventNamespace = type.Namespace?.EndsWith(IntegrationEventsNamespace) == true ||
                                type.Namespace?.EndsWith(DomainEventsNamespace) == true;

        return hasEventAttribute && hasEventNamespace;
    }

    private static EventMetadata CreateEventMetadata(Type eventType, string defaultDomain, XmlDocumentationParser? xmlParser)
    {
        var topicAttribute = GetEventTopicAttribute<EventTopicAttribute>(eventType);
        var obsoleteAttribute = eventType.GetCustomAttribute<ObsoleteAttribute>();
        var (properties, partitionKeys) = GetEventPropertiesAndPartitionKeys(eventType, xmlParser);

        var topicName = topicAttribute.ShouldPluralizeTopicName ? topicAttribute.Topic.Pluralize() : topicAttribute.Topic;

        var eventDomain = !string.IsNullOrWhiteSpace(topicAttribute.Domain)
            ? topicAttribute.Domain
            : GetDomainFromNamespace(eventType.Namespace) ?? defaultDomain;

        // Build full topic name: {env}.{domain}.{visibility}.{topic}.{version}
        var visibility = topicAttribute.Internal ? "internal" : "external";
        var version = topicAttribute.Version;
        var fullTopicName = $"{{env}}.{defaultDomain.ToLowerInvariant()}.{visibility}.{topicName}.{version}";

        return new EventMetadata
        {
            EventName = eventType.Name,
            FullTypeName = eventType.FullName ?? eventType.Name,
            Namespace = eventType.Namespace ?? string.Empty,
            TopicName = fullTopicName,
            Domain = eventDomain,
            Version = version,
            IsInternal = topicAttribute.Internal,
            EventType = eventType,
            TopicAttribute = GetEventTopicAttribute<Attribute>(eventType),
            Properties = properties,
            PartitionKeys = partitionKeys,
            ObsoleteMessage = obsoleteAttribute?.Message
        };
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

        // If T is Attribute (base type), return any EventTopicAttribute found
        if (typeof(T) == typeof(Attribute) && foundAttribute != null)
        {
            return (T)foundAttribute;
        }

        throw new InvalidOperationException($"EventTopicAttribute not found on type {type.Name}");
    }

    private static (List<EventPropertyMetadata> properties, List<PartitionKeyMetadata> partitionKeys) GetEventPropertiesAndPartitionKeys(
        Type eventType, XmlDocumentationParser? xmlParser)
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
            var isRequired = IsRequiredProperty(property);

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
            var sizeResult = PayloadSizeCalculator.CalculatePropertySize(property, property.PropertyType);

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
                    Order = partitionKeyAttr?.Order ?? 0,
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


    private static bool IsRequiredProperty(PropertyInfo property)
    {
        if (property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>() is not null)
        {
            return true;
        }

        var nullableContext = new NullabilityInfoContext();
        var nullabilityInfo = nullableContext.Create(property);

        return nullabilityInfo.WriteState == NullabilityState.NotNull;
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
}
