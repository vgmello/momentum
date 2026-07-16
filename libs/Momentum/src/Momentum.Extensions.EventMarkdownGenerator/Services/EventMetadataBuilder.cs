// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;
using Momentum.Extensions.Abstractions.Extensions;
using Momentum.Extensions.EventMarkdownGenerator.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

/// <summary>
///     Builds <see cref="EventMetadata"/> for a single discovered event type. Separated from
///     <see cref="AssemblyEventDiscovery"/> so assembly/type scanning stays independent of the
///     (considerably larger) job of reflecting an event type and its topic attribute into metadata.
///     Generic reflection helpers with no metadata-specific meaning live in <see cref="TypeUtils"/>.
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
            EventPropertyMetadataBuilder.Build(eventType, xmlParser, calculator, partitionKeyAttributeNamePrefix);

        // Access properties via reflection for cross-assembly compatibility
        var attrType = topicAttribute.GetType();
        var topic = TypeUtils.GetPropertyValue<string?>(attrType, topicAttribute, "Topic");
        var shouldPluralize = TypeUtils.GetPropertyValue<bool?>(attrType, topicAttribute, "ShouldPluralizeTopicName") ?? false;
        var domain = TypeUtils.GetPropertyValue<string?>(attrType, topicAttribute, "Domain");
        var isInternal = TypeUtils.GetPropertyValue<bool?>(attrType, topicAttribute, "Internal") ?? false;
        var version = TypeUtils.GetPropertyValue<string?>(attrType, topicAttribute, "Version") ?? "v1";
        var eventNameOverride = TypeUtils.GetPropertyValue<string?>(attrType, topicAttribute, "EventName");

        var topicName = !string.IsNullOrEmpty(topic) ? topic : eventType.Name.ToKebabCase();

        // shouldPluralize (ShouldPluralizeTopicName) is the topic-explicit signal: for EventTopicAttribute<T>
        // it's true only when the attribute's own topic ctor argument was null, i.e. Topic was auto-derived
        // from the entity type name rather than explicitly set (Topic itself is never empty by this point —
        // the base ctor defaults it to the kebab-cased entity name even when not explicitly given).
        if (shouldPluralize && !topicName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            topicName = topicName.Pluralize();
        }

        var eventDomain = !string.IsNullOrWhiteSpace(domain)
            ? domain
            : GetDomainFromNamespace(eventType.Namespace) ?? defaultDomain;

        // Build full topic name: {domain}.{visibility}.{topic}.{version}
        var visibility = isInternal ? "internal" : "public";

        var fullTopicName = $"{eventDomain.ToKebabCase()}.{visibility}.{topicName}.{version}";

        var eventName = !string.IsNullOrWhiteSpace(eventNameOverride) ? eventNameOverride : eventType.Name;

        return new EventMetadata
        {
            EventName = eventName,
            EventNameKebab = eventName.ToKebabCase(),
            EventTypeName = eventType.Name,
            FullTypeName = eventType.FullName ?? eventType.Name,
            Namespace = eventType.Namespace ?? string.Empty,
            Topic = topicName,
            FullyQualifiedTopicName = fullTopicName,
            Domain = eventDomain,
            Version = version,
            IsInternal = isInternal,
            TopicAttribute = topicAttribute,
            Entity = GetEntity(attrType, eventType),
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

    // Common past-tense verb suffixes event types are named with (CashierCreated, OrderCompleted, ...).
    // Longer/more specific suffixes are listed first so e.g. "Cancelled" isn't shadowed by a shorter
    // unrelated match.
    private static readonly string[] CommonEventNameSuffixes =
    [
        "Created", "Updated", "Deleted", "Cancelled", "Canceled", "Completed", "Processed", "Published",
        "Received", "Generated", "Activated", "Deactivated", "Registered", "Requested", "Approved",
        "Rejected", "Started", "Finished", "Changed", "Removed", "Added", "Modified"
    ];

    /// <summary>
    ///     Kebab-cases the entity type argument on a generic topic attribute (e.g.
    ///     <c>EventTopicAttribute&lt;Cashier&gt;</c> yields "cashier"). When the attribute isn't generic
    ///     (no TEntity to reflect), falls back to stripping a common event-verb suffix from the event
    ///     type's own name (e.g. "CashierCreated" yields "cashier"); if no known suffix matches, empty.
    /// </summary>
    private static string GetEntity(Type attrType, Type eventType)
    {
        if (attrType.IsGenericType)
        {
            var genericArgs = attrType.GetGenericArguments();

            if (genericArgs.Length > 0)
                return genericArgs[0].Name.ToKebabCase();
        }

        var typeName = eventType.Name;
        var suffix = CommonEventNameSuffixes.FirstOrDefault(s =>
            typeName.Length > s.Length && typeName.EndsWith(s, StringComparison.Ordinal));

        return suffix != null ? typeName[..^suffix.Length].ToKebabCase() : string.Empty;
    }

    private static Attribute GetEventTopicAttributeDynamic(Type type, string attributeNamePrefix)
    {
        var foundAttribute = TypeUtils.FindAttributeByName(type.GetCustomAttributes(), attributeNamePrefix);

        if (foundAttribute == null)
        {
            throw new InvalidOperationException($"Attribute with prefix '{attributeNamePrefix}' not found on type {type.Name}");
        }

        return foundAttribute;
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
}
