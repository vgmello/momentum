namespace Operations.Extensions.EventDocGenerator.Models;

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
    public List<EventPropertyMetadata> Properties { get; init; } = new();
    
    public string GetAnchorId() => $"{EventName.ToLowerInvariant()}-{Version}";
    public string GetDisplayName() => $"{EventName} ({Version})";
}

public record EventPropertyMetadata
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required Type PropertyType { get; init; }
    public string? Description { get; init; }
    public bool IsRequired { get; init; }
    public bool IsComplexType { get; init; }
}