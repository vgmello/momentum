---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# GraphNode

## Description

Represents a graphnode entity.

## Schema

<!-- #region schema -->

| Property | Type | Required | Description |
| -------- | ---- | -------- | ----------- |
| Id| `Guid` | ✓| Gets or sets the id. |
| Name| `string` | ✓| Gets or sets the name. |
| [Parent](/events/schemas/TestEvents.Graph.Nodes.Contracts.Models.GraphNode.md)| `GraphNode` | | Gets or sets the parent. |
| [Children](/events/schemas/TestEvents.Graph.Nodes.Contracts.Models.GraphNode.md)| `List<GraphNode>` | ✓| Gets or sets the children. |
| [Metadata](/events/schemas/TestEvents.Graph.Nodes.Contracts.Models.NodeMetadata.md)| `NodeMetadata` | | Gets or sets the metadata. |


<!-- #endregion schema -->

### Reference Schemas

#### GraphNode
<!--@include: @/events/schemas/TestEvents.Graph.Nodes.Contracts.Models.GraphNode.md#schema-->

#### GraphNode
<!--@include: @/events/schemas/TestEvents.Graph.Nodes.Contracts.Models.GraphNode.md#schema-->

#### NodeMetadata
<!--@include: @/events/schemas/TestEvents.Graph.Nodes.Contracts.Models.NodeMetadata.md#schema-->

