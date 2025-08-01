// Copyright (c) ABCDEG. All rights reserved.

namespace Operations.Extensions.EventMarkdownGenerator.Templates.Models;

public class EventViewModel
{
    public string EventName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public bool IsObsolete { get; set; }
    public bool IsInternal { get; set; }

    public string? ObsoleteMessage { get; set; }
    public string GithubUrl { get; set; } = string.Empty;
    public string TopicAttributeDisplayName { get; set; } = string.Empty;

    // Documentation properties
    public string Description { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string? Example { get; set; }

    public EventPropertyViewModel[] Properties { get; set; } = [];
    public PartitionKeyViewModel[] PartitionKeys { get; set; } = [];
    public int TotalEstimatedSizeBytes { get; set; }
    public bool HasInaccurateEstimates { get; set; }
}
