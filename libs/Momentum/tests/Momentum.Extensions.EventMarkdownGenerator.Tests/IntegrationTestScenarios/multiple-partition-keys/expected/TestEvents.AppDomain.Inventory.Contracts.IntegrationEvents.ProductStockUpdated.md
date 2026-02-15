---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# ProductStockUpdated

- **Status:** Active
- **Version:** v1
- **Entity:** `product`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.products.v1`
- **Estimated Payload Size:** 64 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId, WarehouseId, ProductCategory
## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| WarehouseId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| ProductCategory| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available (partition key) |
| ProductId| `Guid` | ✓| 16 bytes | No description available |
| PreviousQuantity| `int` | ✓| 4 bytes | No description available |
| NewQuantity| `int` | ✓| 4 bytes | No description available |
| UpdatedAt| `DateTime` | ✓| 8 bytes | No description available |


### Partition Keys

This event uses multiple partition keys for message routing:
- `TenantId` - No description available
    - `WarehouseId` - No description available
    - `ProductCategory` - No description available
    ## Technical Details

- **Full Type:** [TestEvents.AppDomain.Inventory.Contracts.IntegrationEvents.ProductStockUpdated](#)
- **Namespace:** `TestEvents.AppDomain.Inventory.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
