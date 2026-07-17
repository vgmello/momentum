// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;
using Momentum.Extensions.Abstractions.Extensions;
using Momentum.Extensions.Abstractions.Messaging;
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
        PayloadSizeCalculator calculator, string attributeNamePrefix, string partitionKeyAttributeNamePrefix,
        bool emitPublicVisibility = false)
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
        var subdomain = TypeUtils.GetPropertyValue<string?>(attrType, topicAttribute, "Subdomain");
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

        var eventDomain = !string.IsNullOrWhiteSpace(domain) ? domain : defaultDomain;

        var eventSubdomain = !string.IsNullOrWhiteSpace(subdomain)
            ? subdomain
            : GetSubdomainFromNamespace(eventType.Namespace);

        // Build full topic name: {visibility}.{domain}.{subdomain}.{topic}.{version}
        // Internal events always render the "internal" segment. Public events only render "public"
        // when explicitly requested (emitPublicVisibility); otherwise the segment is omitted entirely.
        string? visibilitySegment = null;

        if (isInternal)
            visibilitySegment = "internal";
        else if (emitPublicVisibility)
            visibilitySegment = "public";

        var segments = new List<string?>
        {
            visibilitySegment,
            eventDomain.ToKebabCase(),
            eventSubdomain?.ToKebabCase(),
            topicName,
            version
        };

        var fullTopicName = string.Join('.', segments.Where(s => !string.IsNullOrEmpty(s)));

        var eventName = !string.IsNullOrWhiteSpace(eventNameOverride) ? eventNameOverride : eventType.Name;
        var (entityName, entityType) = GetEntity(attrType, eventType, properties);

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
            Subdomain = eventSubdomain,
            Version = version,
            IsInternal = isInternal,
            TopicAttribute = topicAttribute,
            Entity = entityName,
            EntityType = entityType,
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
        "Rejected", "Started", "Finished", "Ended", "Changed", "Removed", "Added", "Modified", "Opened",
        "Closed", "Failed", "Succeeded", "Expired", "Confirmed", "Submitted", "Scheduled"
    ];

    /// <summary>
    ///     Resolves the PascalCase entity name and, when available, the reflectable entity <see cref="Type"/> —
    ///     the type argument of a generic topic attribute (e.g. <c>EventTopicAttribute&lt;Cashier&gt;</c> yields
    ///     ("Cashier", typeof(Cashier))). When the attribute isn't generic (no TEntity to reflect), falls back to
    ///     stripping a common event-verb suffix from the event type's own name (e.g. "CashierCreated" yields
    ///     "Cashier", with no corresponding Type). A trailing "Event" word is trimmed first, before suffix
    ///     matching, so e.g. "OrderCompletedEvent" still yields "Order" rather than failing to match "Completed"
    ///     against "OrderCompletedEvent". If no known suffix matches, falls back further to the event type's own
    ///     name (with the trailing "Event" word, if any, still trimmed) — e.g. "BusinessDayReported" with no
    ///     matching suffix yields "BusinessDayReported", and "WidgetEvent" yields "Widget". In either fallback
    ///     case, if a complex-type payload property's name matches the derived entity name exactly (e.g.
    ///     "CashierCreated" with a "Cashier" property), that property's type is used as the entity Type — this
    ///     is what lets the entity link to a generated schema page even without a generic attribute. The
    ///     generic-argument path only applies to the framework's own <c>EventTopicAttribute&lt;TEntity&gt;</c>
    ///     (matched by name, for cross-assembly-context compatibility) — a custom generic attribute with a
    ///     different name is treated as non-generic and always falls through to the name-derivation/property-match
    ///     path instead.
    /// </summary>
    private static (string Name, Type? Type) GetEntity(Type attrType, Type eventType,
        IReadOnlyList<EventPropertyMetadata> properties)
    {
        var isBuiltInEventTopicAttribute = attrType.Name.StartsWith(nameof(EventTopicAttribute), StringComparison.Ordinal);

        if (attrType.IsGenericType && isBuiltInEventTopicAttribute)
        {
            var genericArgs = attrType.GetGenericArguments();

            if (genericArgs.Length > 0)
                return (genericArgs[0].Name, genericArgs[0]);
        }

        var typeName = eventType.Name;

        if (typeName.Length > "Event".Length && typeName.EndsWith("Event", StringComparison.Ordinal))
            typeName = typeName[..^"Event".Length];

        var suffix = CommonEventNameSuffixes.FirstOrDefault(s =>
            typeName.Length > s.Length && typeName.EndsWith(s, StringComparison.Ordinal));

        var entityName = suffix != null ? typeName[..^suffix.Length] : typeName;

        var matchingProperty = properties.FirstOrDefault(p =>
            p.IsComplexType && string.Equals(p.Name, entityName, StringComparison.Ordinal));

        return (entityName, matchingProperty?.PropertyType);
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

    private static string? GetSubdomainFromNamespace(string? namespaceName)
    {
        if (string.IsNullOrEmpty(namespaceName))
            return null;

        // For namespaces like "AppDomain.Cashiers.Contracts.IntegrationEvents", extract "Cashiers"
        // The pattern is: Domain.Subdomain.[SomeNameSpace].Contracts.IntegrationEvents
        var parts = namespaceName.Split('.');

        var contractsIndex = Array.LastIndexOf(parts, "Contracts");

        return contractsIndex > 0 ? parts[contractsIndex - 1] : null;
    }
}
