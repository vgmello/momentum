---
editLink: false
---

# PaymentProcessed

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `payment`
-   **Type:** Integration Event
-   **Topic:** `{env}.platform.public.payments.v1`
-   **Partition Keys**: TenantId

## Description

Published when a payment is successfully processed

## Event Payload

| Property  | Type      | Required | Description                       |
| --------- | --------- | -------- | --------------------------------- |
| TenantId  | `Guid`    | ✓        | Tenant identifier (partition key) |
| PaymentId | `string`  | ✓        | Payment identifier                |
| Amount    | `decimal` | ✓        | Payment amount                    |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

## Technical Details

-   **Full Type:** [`Platform.AppDomain.Payments.Contracts.IntegrationEvents.PaymentProcessed`](https://[github.url.from.config.com]/Platform/AppDomain/Payments/Contracts/IntegrationEvents/PaymentProcessed.cs)
-   **Namespace:** `Platform.AppDomain.Payments.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic<Payment>]`
