// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Models;

public record EventDocumentation
{
    public required string Summary { get; init; }

    public string? Remarks { get; init; }

    public string? Example { get; init; }

    public Dictionary<string, string> PropertyDescriptions { get; init; } = [];

    public string GetDescription() => !string.IsNullOrEmpty(Summary) ? Summary : "No description available";
}

public record EventWithDocumentation
{
    public required EventMetadata Metadata { get; init; }

    public required EventDocumentation Documentation { get; init; }
}
