namespace Operations.Extensions.EventDocGenerator.Models;

public record EventDocumentation
{
    public required string Summary { get; init; }
    public string? Remarks { get; init; }
    public string? Example { get; init; }
    public Dictionary<string, string> PropertyDescriptions { get; init; } = new();
    
    public string GetTriggerScenario() => !string.IsNullOrEmpty(Remarks) ? Remarks : "Not specified";
}

public record AssemblyEventDocumentation
{
    public required string AssemblyName { get; init; }
    public required string AssemblyPath { get; init; }
    public required string DefaultDomain { get; init; }
    public List<EventWithDocumentation> PublicEvents { get; init; } = new();
    public List<EventWithDocumentation> InternalEvents { get; init; } = new();
    
    public IEnumerable<EventWithDocumentation> AllEvents => PublicEvents.Concat(InternalEvents);
    
    public Dictionary<string, List<EventWithDocumentation>> GetEventsByTopic()
    {
        return AllEvents
            .GroupBy(e => e.Metadata.EventName)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Metadata.Version).ToList());
    }
}

public record EventWithDocumentation
{
    public required EventMetadata Metadata { get; init; }
    public required EventDocumentation Documentation { get; init; }
}