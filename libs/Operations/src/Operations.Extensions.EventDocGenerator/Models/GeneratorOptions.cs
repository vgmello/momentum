namespace Operations.Extensions.EventDocGenerator.Models;

public record GeneratorOptions
{
    public required string AssemblyPath { get; init; }
    public required string OutputDirectory { get; init; }
    public required string FileName { get; init; }
    public string? XmlDocumentationPath { get; init; }
    public string Environment { get; init; } = "dev";
    public bool GenerateMarkdown { get; init; } = true;
    public bool GenerateJsonSidebar { get; init; } = true;
    public bool Verbose { get; init; } = false;
    
    public string GetMarkdownPath() => Path.Combine(OutputDirectory, FileName);
    public string GetJsonSidebarPath() => Path.Combine(OutputDirectory, Path.ChangeExtension(FileName, ".sidebar.json"));
}

public record SidebarItem
{
    public required string Text { get; init; }
    public string? Link { get; init; }
    public List<SidebarItem> Items { get; init; } = new();
    public bool Collapsed { get; init; } = false;
}

public record VitePressOutput
{
    public required string MarkdownContent { get; init; }
    public required SidebarItem SidebarConfiguration { get; init; }
}