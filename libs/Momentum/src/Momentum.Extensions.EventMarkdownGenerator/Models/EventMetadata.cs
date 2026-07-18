// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Extensions;

namespace Momentum.Extensions.EventMarkdownGenerator.Models;

public record EventMetadata
{
    public required string EventName { get; init; }

    /// <summary>
    ///     Kebab-cased form of <see cref="EventName"/> (respects an <c>EventName</c> attribute override
    ///     the same way EventName itself does). Intended for topic-adjacent/URL-safe uses in templates
    ///     that want a slug without kebab-casing the human-facing heading itself.
    /// </summary>
    public required string EventNameKebab { get; init; }

    public required string EventTypeName { get; init; }
    public required string FullTypeName { get; init; }
    public required string Namespace { get; init; }
    public required string Topic { get; init; }
    public required string FullyQualifiedTopicName { get; init; }
    public required string Domain { get; init; }
    public string? Subdomain { get; init; }
    public required string Version { get; init; }
    public required bool IsInternal { get; init; }
    public required Attribute TopicAttribute { get; init; }

    /// <summary>
    ///     PascalCase entity name: the type argument on a generic topic attribute (e.g.
    ///     <c>EventTopicAttribute&lt;Cashier&gt;</c> yields "Cashier"), or, when the attribute isn't generic,
    ///     the event type's own name with a common event-verb suffix stripped (e.g. "CashierCreated" yields
    ///     "Cashier").
    /// </summary>
    public required string Entity { get; init; }

    /// <summary>
    ///     The reflectable entity <see cref="Type"/>: the type argument reflected from a generic topic
    ///     attribute, or, when the attribute isn't generic, a payload property's type when that property's
    ///     name exactly matches <see cref="Entity"/>. <c>null</c> when neither resolves. Used to link
    ///     <see cref="Entity"/> to its schema documentation when one was generated for this type.
    /// </summary>
    public Type? EntityType { get; init; }

    /// <summary>
    ///     Every public property of <see cref="TopicAttribute"/>, name to stringified value, discovered via
    ///     reflection. Captures custom/unknown attribute properties (beyond the well-known ones already
    ///     mapped onto this record, e.g. Domain/Version/Topic) so templates can render them without the
    ///     generator needing to know about them ahead of time.
    /// </summary>
    public IReadOnlyDictionary<string, string> AttributeProperties { get; init; } = new Dictionary<string, string>();

    public List<EventPropertyMetadata> Properties { get; init; } = [];
    public List<PartitionKeyMetadata> PartitionKeys { get; init; } = [];
    public string? ObsoleteMessage { get; init; }
    public bool IsObsolete => !string.IsNullOrEmpty(ObsoleteMessage);

    public string GetAnchorId() => $"{EventName.ToLowerInvariant()}-{Version}";
    public string GetDisplayName() => $"{EventName} ({Version})";
    public string GetTypeNameFileName() => $"{FullTypeName.ToSafeFileName()}.md";
    public string GetFileName() => GetTypeNameFileName();
    public string GetStatus() => IsObsolete ? "Deprecated" : "Active";

    /// <summary>
    ///     The output subfolder this event's markdown belongs under: <c>domain_events</c> for events marked
    ///     <c>Internal</c> on their topic attribute, <c>integration_events</c> otherwise. Reuses
    ///     <see cref="IsInternal"/> (rather than inspecting the namespace for an
    ///     IntegrationEvents/DomainEvents segment) so placement always matches the "Domain Events" sidebar
    ///     grouping <see cref="Momentum.Extensions.EventMarkdownGenerator.Services.JsonSidebarGenerator"/>
    ///     already produces from the same flag.
    /// </summary>
    public string GetEventKindFolder() => IsInternal ? "domain_events" : "integration_events";
}

public record EventPropertyMetadata
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required Type PropertyType { get; init; }
    public string? Description { get; init; }
    public bool IsRequired { get; init; }
    public bool IsComplexType { get; init; }
    public bool IsPartitionKey { get; init; }
    public int? PartitionKeyOrder { get; init; }
    public int EstimatedSizeBytes { get; init; }
    public bool IsAccurate { get; init; } = true;
    public string? SizeWarning { get; init; }
}

public record PartitionKeyMetadata
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public string? Description { get; init; }
    public int Order { get; init; }
    public bool IsFromParameter { get; init; }
}
