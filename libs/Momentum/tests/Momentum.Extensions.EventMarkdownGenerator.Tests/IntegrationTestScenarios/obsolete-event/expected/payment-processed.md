---
editLink: false
---

# PaymentProcessed

> [!CAUTION]
> This event is deprecated. Please use PaymentCompleted event instead.

-   **Status:** Deprecated
-   **Version:** v1
-   **Entity:** `payment`
-   **Type:** Integration Event
-   **Topic:** `{env}.AppDomain.public.legacy.v1`
-   **Partition Keys**: TenantId

## Description

Published when a payment is processed through the legacy payment system

## When It's Triggered

This event was published when:

-   Payment was processed through the old payment gateway
-   Transaction was completed successfully

## Migration Notes

This event has been replaced by the new PaymentCompleted event which provides:

-   Enhanced payment method tracking
-   Better error handling and retry logic
-   Improved audit trail capabilities

## Event Payload

| Property      | Type      | Required | Description                              |
| ------------- | --------- | -------- | ---------------------------------------- |
| TenantId      | `Guid`    | ✓        | Identifier of the tenant (partition key) |
| Amount        | `decimal` | ✓        | Payment amount that was processed        |
| TransactionId | `string`  | ✓        | Legacy transaction identifier            |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

## Technical Details

-   **Full Type:** [`AppDomain.Legacy.Contracts.IntegrationEvents.PaymentProcessed`](https://[github.url.from.config.com]/AppDomain/Legacy/Contracts/IntegrationEvents/PaymentProcessed.cs)
-   **Namespace:** `AppDomain.Legacy.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic<Payment>]`
