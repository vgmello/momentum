// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Extensions;

namespace Momentum.Extensions.EventMarkdownGenerator.Models;

public record EventMetadata
{
    public required string EventName { get; init; }
    public required string EventTypeName { get; init; }
    public required string FullTypeName { get; init; }
    public required string Namespace { get; init; }
    public required string Topic { get; init; }
    public required string FullyQualifiedTopicName { get; init; }
    public required string Domain { get; init; }
    public required string Version { get; init; }
    public required bool IsInternal { get; init; }
    public required Attribute TopicAttribute { get; init; }

    /// <summary>
    ///     Kebab-cased name of the entity type argument on a generic topic attribute (e.g.
    ///     <c>EventTopicAttribute&lt;Cashier&gt;</c> yields "cashier"), or empty when the topic
    ///     attribute isn't generic.
    /// </summary>
    public required string Entity { get; init; }

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
