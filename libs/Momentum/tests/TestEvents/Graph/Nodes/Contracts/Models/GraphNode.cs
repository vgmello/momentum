// Copyright (c) Momentum .NET. All rights reserved.

namespace TestEvents.Graph.Nodes.Contracts.Models;

/// <summary>
///     Represents a node in a graph structure with potential circular references
/// </summary>
public record GraphNode
{
    /// <summary>
    ///     Unique identifier for the graph node
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Display name of the graph node
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Reference to the parent node (null for root nodes)
    /// </summary>
    public GraphNode? Parent { get; init; }

    /// <summary>
    ///     Collection of child nodes that reference this node as their parent
    /// </summary>
    public List<GraphNode> Children { get; init; } = new();

    /// <summary>
    ///     Additional node metadata and properties
    /// </summary>
    public NodeMetadata? Metadata { get; init; }
}
