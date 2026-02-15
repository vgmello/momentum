---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# CashierCreated

- **Status:** Active
- **Version:** v1
- **Entity:** `cashier`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.cashiers.v1`
- **Estimated Payload Size:** 73 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId, PartitionKeyTest

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| PartitionKeyTest| `int` | ✓| 4 bytes | No description available (partition key) |
| [Cashier](/events/schemas/TestEvents.AppDomain.Cashiers.Contracts.IntegrationEvents.Cashier.md)| `Cashier` | ✓| 53 bytes (Name: Dynamic size - no MaxLength constraint, Email: Dynamic size - no MaxLength constraint) | No description available |


### Partition Keys

This event uses multiple partition keys for message routing:
- `TenantId` - No description available
    - `PartitionKeyTest` - No description available
    
### Reference Schemas

#### Cashier

<!--@include: @/events/schemas/TestEvents.AppDomain.Cashiers.Contracts.IntegrationEvents.Cashier.md#schema-->

## Technical Details

- **Full Type:** [TestEvents.AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated](#)
- **Namespace:** `TestEvents.AppDomain.Cashiers.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
