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

    /// <summary>
    ///     Optional path to the source checkout root (the parent of each project's folder, e.g. the parent of
    ///     <c>src/AppDomain/</c>). When set, an event's GitHub source link is only emitted if the guessed file
    ///     actually exists under this root — otherwise it falls back to <c>"#"</c> rather than risk a
    ///     confidently wrong link (e.g. a type declared in a file whose name doesn't match the type's own
    ///     name). When unset, the link is always emitted best-effort, unverified.
    /// </summary>
    public string? SourceRootDirectory { get; init; }

    /// <summary>Serialization format for overhead calculation. Default: "json". Options: "json", "binary".</summary>
    public string SerializationFormat { get; init; } = "json";

    /// <summary>Name (or name prefix) of the attribute used to discover events. Default: "EventTopicAttribute".</summary>
    public string EventAttributeName { get; init; } = "EventTopicAttribute";

    /// <summary>Name (or name prefix) of the attribute used to discover partition keys. Default: "PartitionKeyAttribute".</summary>
    public string PartitionKeyAttributeName { get; init; } = "PartitionKeyAttribute";

    /// <summary>
    ///     Whether public events should render an explicit "public" visibility segment in
    ///     <see cref="EventMetadata.FullyQualifiedTopicName"/>. Default: <c>false</c> (public events omit the
    ///     segment entirely, e.g. <c>domain.subdomain.topic.v1</c>). Internal events always render "internal"
    ///     regardless of this flag, e.g. <c>internal.domain.subdomain.topic.v1</c>.
    /// </summary>
    public bool EmitPublicVisibility { get; init; }

    public string GetSidebarPath() => Path.Combine(OutputDirectory, Path.GetFileName(SidebarFileName));

    public void EnsureOutputDirectoryExists()
    {
        if (!Directory.Exists(OutputDirectory))
            Directory.CreateDirectory(OutputDirectory);
    }
}
