// Copyright (c) ABCDEG. All rights reserved.

using Operations.Extensions.EventMarkdownGenerator.Extensions;

namespace Operations.Extensions.EventMarkdownGenerator.Models;

public record EventMetadata
{
    public required string EventName { get; init; }
    public required string FullTypeName { get; init; }
    public required string Namespace { get; init; }
    public required string TopicName { get; init; }
    public required string Domain { get; init; }
    public required string Version { get; init; }
    public required bool IsInternal { get; init; }
    public required Type EventType { get; init; }
    public required Attribute TopicAttribute { get; init; }
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
