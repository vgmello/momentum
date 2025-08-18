# GraphNode

Represents a node in a graph structure with potential circular references.

## Properties

| Property | Type | Required | Description |
| ----------------------------------------------------------------- | --------- | -------- | --------------------------------------------------------------------- |
| Id | `Guid` | ✓ | Unique identifier for the graph node |
| Name | `string` | ✓ | Display name of the graph node |
| Parent | `GraphNode?` |  | Reference to the parent node (null for root nodes) |
| Children | `List<GraphNode>` | ✓ | Collection of child nodes that reference this node as their parent |
| [Metadata](./Graph.Nodes.Contracts.Models.NodeMetadata.md) | `NodeMetadata` | ✓ | Additional node metadata and properties |