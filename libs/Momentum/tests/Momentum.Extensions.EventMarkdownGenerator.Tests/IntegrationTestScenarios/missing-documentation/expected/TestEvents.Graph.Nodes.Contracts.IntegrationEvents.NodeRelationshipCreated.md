---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# NodeRelationshipCreated

- **Status:** Active
- **Version:** v1
- **Entity:** `node-relationship-created`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.node-relationship-createds.v1`
- **Estimated Payload Size:** 382 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| [SourceNode](/events/schemas/TestEvents.Graph.Nodes.Contracts.Models.GraphNode.md)| `GraphNode` | ✓| 183 bytes (Name: Dynamic size - no MaxLength constraint, Parent: Circular reference detected, Children: Collection size estimated (no Range constraint), Metadata: CreatedBy: Dynamic size - no MaxLength constraint, Tags: Collection size estimated (no Range constraint), OwnerNode: Circular reference detected) | No description available |
| [TargetNode](/events/schemas/TestEvents.Graph.Nodes.Contracts.Models.GraphNode.md)| `GraphNode` | ✓| 183 bytes (Name: Dynamic size - no MaxLength constraint, Parent: Circular reference detected, Children: Collection size estimated (no Range constraint), Metadata: CreatedBy: Dynamic size - no MaxLength constraint, Tags: Collection size estimated (no Range constraint), OwnerNode: Circular reference detected) | No description available |
| RelationshipType| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |


### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available
    
### Reference Schemas

#### GraphNode

<!--@include: @/events/schemas/TestEvents.Graph.Nodes.Contracts.Models.GraphNode.md#schema-->

#### GraphNode

<!--@include: @/events/schemas/TestEvents.Graph.Nodes.Contracts.Models.GraphNode.md#schema-->

## Technical Details

- **Full Type:** [TestEvents.Graph.Nodes.Contracts.IntegrationEvents.NodeRelationshipCreated](#)
- **Namespace:** `TestEvents.Graph.Nodes.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
