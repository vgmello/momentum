---
editLink: false
---

# PaymentStatusChanged

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `payment`
-   **Type:** Integration Event
-   **Topic:** `{env}.AppDomain.public.types.v1`
-   **Partition Keys**: TenantId

## Description

Published when payment status transitions occur with optional metadata and enum values

## When It's Triggered

This event is published when:

-   Payment status changes from one state to another
-   Optional payment metadata is updated
-   Nullable fields are conditionally populated

## Type System Features

This event demonstrates:

-   Nullable reference types with proper nullability annotations
-   Enum types for type-safe status representation
-   Optional nullable value types for conditional data
-   Mixed required and optional fields

## Event Payload

| Property       | Type             | Required | Description                                                                      |
| -------------- | ---------------- | -------- | -------------------------------------------------------------------------------- |
| TenantId       | `Guid`           | ✓        | Identifier of the tenant that owns the payment (partition key)                   |
| PaymentId      | `string`         | ✓        | Unique identifier for the payment being updated                                  |
| PreviousStatus | `PaymentStatus`  | ✓        | The payment status before this change occurred                                   |
| NewStatus      | `PaymentStatus`  | ✓        | The new payment status after the change                                          |
| ProcessedAt    | `DateTime?`      |          | Optional timestamp when payment processing completed (null for pending statuses) |
| Notes          | `string?`        |          | Optional notes about the status change (null if no additional information)       |
| Amount         | `decimal?`       |          | Optional payment amount (null for status changes that don't involve amounts)     |
| PaymentMethod  | `PaymentMethod`  | ✓        | The method used for payment processing                                           |
| FailureReason  | `FailureReason?` |          | Optional reason for payment failure (null for successful payments)               |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

## Technical Details

-   **Full Type:** [`AppDomain.Types.Contracts.IntegrationEvents.PaymentStatusChanged`](https://[github.url.from.config.com]/AppDomain/Types/Contracts/IntegrationEvents/PaymentStatusChanged.cs)
-   **Namespace:** `AppDomain.Types.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic<Payment>]`
