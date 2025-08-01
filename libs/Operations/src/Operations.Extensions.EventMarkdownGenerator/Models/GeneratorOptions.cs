// Copyright (c) ABCDEG. All rights reserved.

namespace Operations.Extensions.EventMarkdownGenerator.Models;

public record GeneratorOptions
{
    public List<string> AssemblyPaths { get; init; } = [];

    public List<string> XmlDocumentationPaths { get; init; } = [];

    public required string OutputDirectory { get; init; }

    public required string SidebarFileName { get; init; }

    public string? TemplatesDirectory { get; init; }

    public string? GitHubBaseUrl { get; init; }

    public string GetSidebarPath() => Path.Combine(OutputDirectory, Path.GetFileName(SidebarFileName));

    public void EnsureOutputDirectoryExists()
    {
        if (!Directory.Exists(OutputDirectory))
            Directory.CreateDirectory(OutputDirectory);
    }
}
