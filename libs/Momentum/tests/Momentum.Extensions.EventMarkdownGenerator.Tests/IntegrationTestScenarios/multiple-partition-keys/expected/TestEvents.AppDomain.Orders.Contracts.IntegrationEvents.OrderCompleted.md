---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# OrderCompleted

- **Status:** Active
- **Version:** v1
- **Entity:** `order`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.orders.v1`
- **Estimated Payload Size:** 1388 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId
## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| OrderId| `Guid` | ✓| 16 bytes | No description available |
| OrderNumber| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| [Customer](/events/schemas/TestEvents.AppDomain.Orders.Contracts.Models.Customer.md)| `Customer` | ✓| 121 bytes (Name: Dynamic size - no MaxLength constraint, Email: Dynamic size - no MaxLength constraint, AppDomainAddress: Street: Dynamic size - no MaxLength constraint, City: Dynamic size - no MaxLength constraint, State: Dynamic size - no MaxLength constraint, PostalCode: Dynamic size - no MaxLength constraint, Country: Dynamic size - no MaxLength constraint) | No description available |
| [Items](/events/schemas/TestEvents.AppDomain.Orders.Contracts.Models.OrderItem.md)| `List<OrderItem>` | ✓| 1211 bytes (Collection size estimated (no Range constraint)) | No description available |
| TotalAmount| `decimal` | ✓| 16 bytes | No description available |
| CompletedAt| `DateTime` | ✓| 8 bytes | No description available |


### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available
    
### Reference Schemas

#### Customer

<!--@include: @/events/schemas/TestEvents.AppDomain.Orders.Contracts.Models.Customer.md#schema-->

#### OrderItems

<!--@include: @/events/schemas/TestEvents.AppDomain.Orders.Contracts.Models.OrderItem.md#schema-->

## Technical Details

- **Full Type:** [TestEvents.AppDomain.Orders.Contracts.IntegrationEvents.OrderCompleted](#)
- **Namespace:** `TestEvents.AppDomain.Orders.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
