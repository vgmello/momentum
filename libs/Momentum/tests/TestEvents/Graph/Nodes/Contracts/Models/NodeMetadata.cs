// Copyright (c) Momentum .NET. All rights reserved.

namespace Graph.Nodes.Contracts.Models;

/// <summary>
///     Contains metadata about a graph node
/// </summary>
public record NodeMetadata
{
    /// <summary>
    ///     When the node was created
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    ///     Who created the node
    /// </summary>
    public required string CreatedBy { get; init; }

    /// <summary>
    ///     List of tags associated with the node
    /// </summary>
    public List<string> Tags { get; init; } = new();

    /// <summary>
    ///     Reference back to the node that owns this metadata (circular reference)
    /// </summary>
    public GraphNode? OwnerNode { get; init; }
}
