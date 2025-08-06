# NodeMetadata

Contains metadata about a graph node.

## Properties

| Property | Type | Required | Description |
| ----------------------------------------------------------------- | --------- | -------- | --------------------------------------------------------------------- |
| CreatedAt | `DateTime` | ✓ | When the node was created |
| CreatedBy | `string` | ✓ | Who created the node |
| Tags | `List<string>` | ✓ | List of tags associated with the node |
| [OwnerNode](./Graph.Nodes.Contracts.Models.GraphNode.md) | `GraphNode` | ✓ | Reference back to the node that owns this metadata (circular reference) |