---
editLink: false
---

# ProductStockUpdated

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `product`
-   **Type:** Integration Event
-   **Topic:** `{env}.AppDomain.public.inventory.v1`
-   **Partition Keys**: TenantId, WarehouseId, ProductCategory

## Description

Published when product stock levels are updated in the inventory system

## When It's Triggered

This event is published when:

-   Product stock is adjusted manually
-   Automated stock replenishment occurs
-   Stock is reserved for orders
-   Stock reservations are released

## Partition Strategy

This event uses multiple partition keys to optimize message routing:

-   Primary partitioning by tenant for isolation
-   Secondary partitioning by warehouse for regional processing
-   Tertiary partitioning by product category for specialized handlers

## Event Payload

| Property         | Type       | Required | Description                                                        |
| ---------------- | ---------- | -------- | ------------------------------------------------------------------ |
| TenantId         | `Guid`     | ✓        | Identifier of the tenant that owns the inventory (partition key)   |
| WarehouseId      | `Guid`     | ✓        | Identifier of the warehouse where stock is managed (partition key) |
| ProductCategory  | `string`   | ✓        | Category of the product for specialized processing (partition key) |
| ProductId        | `Guid`     | ✓        | Unique identifier of the product                                   |
| PreviousQuantity | `int`      | ✓        | Stock quantity before the update                                   |
| NewQuantity      | `int`      | ✓        | Stock quantity after the update                                    |
| UpdatedAt        | `DateTime` | ✓        | Date and time when the stock was updated                           |

### Partition Keys

This event uses multiple partition keys for message routing:

-   `TenantId` - Primary partition key based on tenant
-   `WarehouseId` - Secondary partition key for message routing
-   `ProductCategory` - Secondary partition key for message routing

## Technical Details

-   **Full Type:** [`AppDomain.Inventory.Contracts.IntegrationEvents.ProductStockUpdated`](https://[github.url.from.config.com]/AppDomain/Inventory/Contracts/IntegrationEvents/ProductStockUpdated.cs)
-   **Namespace:** `AppDomain.Inventory.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic<Product>]`
