---
editLink: false
---

# OrderCompleted

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `order`
-   **Type:** Integration Event
-   **Topic:** `{env}.AppDomain.public.orders.v1`
-   **Partition Keys**: TenantId

## Description

Published when an order is successfully completed and payment is processed

## When It's Triggered

This event is published when:

-   Payment processing completes successfully
-   All order items are confirmed as available
-   Order status is updated to completed

## Business Impact

This event triggers:

-   Inventory reduction for ordered items
-   Customer notification emails
-   Analytics and reporting updates
-   Loyalty points calculation

## Event Payload

| Property                                                            | Type              | Required | Description                                                            |
| ------------------------------------------------------------------- | ----------------- | -------- | ---------------------------------------------------------------------- |
| TenantId                                                            | `Guid`            | ✓        | Identifier of the tenant that owns the order (partition key)           |
| OrderId                                                             | `Guid`            | ✓        | Unique identifier for the completed order                              |
| OrderNumber                                                         | `string`          | ✓        | Human-readable order number for customer reference                     |
| [Customer](./schemas/AppDomain.Orders.Contracts.Models.Customer.md) | `Customer`        | ✓        | Complete customer information including AppDomain and shipping details |
| Items                                                               | `List<OrderItem>` | ✓        | List of all items included in the completed order                      |
| TotalAmount                                                         | `decimal`         | ✓        | Final total amount charged to the customer                             |
| CompletedAt                                                         | `DateTime`        | ✓        | Date and time when the order was completed                             |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

### Reference Schemas

#### Customers

<!--@include: ./schemas/AppDomain.Orders.Contracts.Models.Customer.md#schema-->

#### OrderItems

<!--@include: ./schemas/AppDomain.Orders.Contracts.Models.OrderItem.md#schema-->

## Technical Details

-   **Full Type:** [`AppDomain.Orders.Contracts.IntegrationEvents.OrderCompleted`](https://[github.url.from.config.com]/AppDomain/Orders/Contracts/IntegrationEvents/OrderCompleted.cs)
-   **Namespace:** `AppDomain.Orders.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic<Order>]`
