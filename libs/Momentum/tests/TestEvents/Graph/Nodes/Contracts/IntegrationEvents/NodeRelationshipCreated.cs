// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;
using TestEvents.Graph.Nodes.Contracts.Models;

namespace TestEvents.Graph.Nodes.Contracts.IntegrationEvents;

/// <summary>
///     Published when a new relationship is created between graph nodes
/// </summary>
/// <param name="TenantId">Identifier of the tenant that owns the graph</param>
/// <param name="SourceNode">The source node in the relationship</param>
/// <param name="TargetNode">The target node in the relationship</param>
/// <param name="RelationshipType">Type of relationship being created</param>
/// <remarks>
///     ## When It's Triggered
/// 
///     This event is published when:
///     - New relationships are established between nodes
///     - Graph topology changes require notification
///     - Circular dependencies are intentionally created
/// 
///     ## Circular Reference Handling
/// 
///     This event contains types that reference each other in a circular manner:
///     - Node references Parent node
///     - Parent node references Child nodes
///     - Child nodes reference their Parent
///     - This tests the circular reference detection in schema generation
/// </remarks>
[EventTopic<NodeRelationshipCreated>]
public sealed record NodeRelationshipCreated(
    [PartitionKey(Order = 0)] Guid TenantId,
    GraphNode SourceNode,
    GraphNode TargetNode,
    string RelationshipType
);
