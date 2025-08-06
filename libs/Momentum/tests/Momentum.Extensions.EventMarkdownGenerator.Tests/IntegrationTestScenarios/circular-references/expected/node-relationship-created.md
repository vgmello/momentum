---
editLink: false
---

# NodeRelationshipCreated

- **Status:** Active
- **Version:** v1
- **Entity:** `node`
- **Type:** Integration Event
- **Topic:** `{env}.graph.public.nodes.v1`
- **Partition Keys**: TenantId

## Description

Published when a new relationship is created between graph nodes

## When It's Triggered

This event is published when:
- New relationships are established between nodes
- Graph topology changes require notification
- Circular dependencies are intentionally created

## Circular Reference Handling

This event contains types that reference each other in a circular manner:
- Node references Parent node
- Parent node references Child nodes
- Child nodes reference their Parent
- This tests the circular reference detection in schema generation

## Event Payload

| Property | Type | Required | Description |
| ----------------------------------------------------------------- | --------- | -------- | --------------------------------------------------------------------- |
| TenantId | `Guid` | ✓ | Identifier of the tenant that owns the graph (partition key) |
| [SourceNode](./schemas/Graph.Nodes.Contracts.Models.GraphNode.md) | `GraphNode` | ✓ | The source node in the relationship |
| [TargetNode](./schemas/Graph.Nodes.Contracts.Models.GraphNode.md) | `GraphNode` | ✓ | The target node in the relationship |
| RelationshipType | `string` | ✓ | Type of relationship being created |

### Partition Keys

This event uses a partition key for message routing:

- `TenantId` - Primary partition key based on tenant

### Reference Schemas

#### GraphNodes

<!--@include: ./schemas/Graph.Nodes.Contracts.Models.GraphNode.md#schema-->

#### NodeMetadatas

<!--@include: ./schemas/Graph.Nodes.Contracts.Models.NodeMetadata.md#schema-->

## Technical Details

- **Full Type:** [`Graph.Nodes.Contracts.IntegrationEvents.NodeRelationshipCreated`](https://[github.url.from.config.com]/Graph/Nodes/Contracts/IntegrationEvents/NodeRelationshipCreated.cs)
- **Namespace:** `Graph.Nodes.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<Node>]`