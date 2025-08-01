using System.Reflection;
using Operations.Extensions.Abstractions.Extensions;
using Operations.Extensions.Abstractions.Messaging;
using Operations.Extensions.EventDocGenerator.Models;
using Operations.ServiceDefaults.Extensions;

namespace Operations.Extensions.EventDocGenerator.Services;

public class AssemblyEventDiscovery
{
    private const string IntegrationEventsNamespace = ".IntegrationEvents";

    public IEnumerable<EventMetadata> DiscoverEvents(Assembly assembly, string environment = "dev")
    {
        var defaultDomain = GetDefaultDomain(assembly);
        var integrationEventTypes = GetIntegrationEventTypes(assembly);

        return integrationEventTypes.Select(type => CreateEventMetadata(type, defaultDomain, environment));
    }

    private static IEnumerable<Type> GetIntegrationEventTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(IsIntegrationEventType)
            .Where(HasEventTopicAttribute);
    }

    private static bool IsIntegrationEventType(Type type)
    {
        return type.Namespace?.EndsWith(IntegrationEventsNamespace) == true;
    }

    private static bool HasEventTopicAttribute(Type type)
    {
        return type.GetCustomAttribute<EventTopicAttribute>() != null ||
               type.GetCustomAttributes().Any(attr => attr.GetType().Name.StartsWith("EventTopicAttribute"));
    }

    private static EventMetadata CreateEventMetadata(Type eventType, string defaultDomain, string environment)
    {
        var topicAttribute = GetEventTopicAttribute(eventType);
        var properties = GetEventProperties(eventType);

        var domain = !string.IsNullOrWhiteSpace(topicAttribute.Domain)
            ? topicAttribute.Domain
            : defaultDomain;

        var scope = topicAttribute.Internal ? "internal" : "public";
        var topicName = topicAttribute.ShouldPluralizeTopicName 
            ? topicAttribute.Topic.Pluralize() 
            : topicAttribute.Topic;

        var envName = environment switch
        {
            "Development" => "dev",
            "Production" => "prod",
            "Test" => "test",
            _ => environment.ToLowerInvariant()
        };

        var versionSuffix = string.IsNullOrWhiteSpace(topicAttribute.Version) ? null : $".{topicAttribute.Version}";
        var fullTopicName = $"{envName}.{domain}.{scope}.{topicName}{versionSuffix}".ToLowerInvariant();

        return new EventMetadata
        {
            EventName = eventType.Name,
            FullTypeName = eventType.FullName ?? eventType.Name,
            Namespace = eventType.Namespace ?? string.Empty,
            TopicName = fullTopicName,
            Domain = domain,
            Version = topicAttribute.Version,
            IsInternal = topicAttribute.Internal,
            EventType = eventType,
            Properties = properties
        };
    }

    private static EventTopicAttribute GetEventTopicAttribute(Type type)
    {
        var attribute = type.GetCustomAttribute<EventTopicAttribute>();
        if (attribute != null)
        {
            return attribute;
        }

        // Handle generic EventTopicAttribute<T>
        var genericAttribute = type.GetCustomAttributes()
            .FirstOrDefault(attr => attr.GetType().Name.StartsWith("EventTopicAttribute") && attr.GetType().IsGenericType);

        if (genericAttribute != null)
        {
            return (EventTopicAttribute)genericAttribute;
        }

        throw new InvalidOperationException($"EventTopicAttribute not found on type {type.Name}");
    }

    private static List<EventPropertyMetadata> GetEventProperties(Type eventType)
    {
        var properties = new List<EventPropertyMetadata>();

        foreach (var property in eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var isComplexType = !IsSimpleType(property.PropertyType);
            var isRequired = IsRequiredProperty(property);

            properties.Add(new EventPropertyMetadata
            {
                Name = property.Name,
                TypeName = GetFriendlyTypeName(property.PropertyType),
                PropertyType = property.PropertyType,
                IsRequired = isRequired,
                IsComplexType = isComplexType
            });
        }

        return properties;
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type)!);
    }

    private static bool IsRequiredProperty(PropertyInfo property)
    {
        // Check for required keyword (C# 11+)
        if (property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>() != null)
        {
            return true;
        }

        // Check for nullable reference types
        var nullableContext = new System.Reflection.NullabilityInfoContext();
        var nullabilityInfo = nullableContext.Create(property);
        
        return nullabilityInfo.WriteState == System.Reflection.NullabilityState.NotNull;
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(DateTime)) return "DateTime";
        if (type == typeof(DateTimeOffset)) return "DateTimeOffset";
        if (type == typeof(TimeSpan)) return "TimeSpan";
        if (type == typeof(Guid)) return "Guid";

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return GetFriendlyTypeName(underlyingType) + "?";
        }

        // Handle generic types
        if (type.IsGenericType)
        {
            var genericTypeName = type.Name.Substring(0, type.Name.IndexOf('`'));
            var genericArgs = type.GetGenericArguments().Select(GetFriendlyTypeName);
            return $"{genericTypeName}<{string.Join(", ", genericArgs)}>";
        }

        return type.Name;
    }

    private static string GetDefaultDomain(Assembly assembly)
    {
        var domainAttribute = assembly.GetCustomAttribute<DefaultDomainAttribute>();
        return domainAttribute?.Domain ?? "unknown";
    }
}