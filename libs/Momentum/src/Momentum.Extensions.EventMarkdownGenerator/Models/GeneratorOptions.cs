// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Models;

public record GeneratorOptions
{
    public List<string> AssemblyPaths { get; init; } = [];

    public List<string> XmlDocumentationPaths { get; init; } = [];

    public required string OutputDirectory { get; init; }

    public required string SidebarFileName { get; init; }

    public string? TemplatesDirectory { get; init; }

    public string? GitHubBaseUrl { get; init; }

    /// <summary>Serialization format for overhead calculation. Default: "json". Options: "json", "binary".</summary>
    public string SerializationFormat { get; init; } = "json";

    public string GetSidebarPath() => Path.Combine(OutputDirectory, Path.GetFileName(SidebarFileName));

    public void EnsureOutputDirectoryExists()
    {
        if (!Directory.Exists(OutputDirectory))
            Directory.CreateDirectory(OutputDirectory);
    }
}
