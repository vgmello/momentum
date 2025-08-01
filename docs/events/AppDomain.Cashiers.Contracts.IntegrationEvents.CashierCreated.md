---
editLink: false
---

# CashierCreated

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `cashier`
-   **Type:** Integration Event
-   **Topic:** `{env}.AppDomain.external.cashiers.v1`
-   **Estimated Payload Size:** 216 bytes ⚠️ _Contains dynamic properties_
-   **Partition Keys**: TenantId, PartitionKeyTest

## Description

Published when a new cashier is successfully created in the AppDomain system. This event contains the complete cashier data and partition
key information for proper message routing.

## When It's Triggered

This event is published when: - The cashier creation process completes successfully

## Some other event data

Some other event data text

## Event Payload

| Property                                                            | Type      | Required | Size                                                                                                                                                                      | Description                                                           |
| ------------------------------------------------------------------- | --------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| TenantId                                                            | `Guid`    | ✓        | 16 bytes                                                                                                                                                                  | Unique identifier for the tenant (partition key)                      |
| PartitionKeyTest                                                    | `int`     | ✓        | 4 bytes                                                                                                                                                                   | Additional partition key for message routing (partition key)          |
| [Cashier](./schemas/AppDomain.Cashiers.Contracts.Models.Cashier.md) | `Cashier` | ✓        | 196 bytes (Name: Dynamic size - no MaxLength constraint, Email: Dynamic size - no MaxLength constraint, CashierPayments: Collection size estimated (no Range constraint)) | Complete cashier object containing all cashier data and configuration |

### Partition Keys

This event uses multiple partition keys for message routing:

-   `TenantId` - Primary partition key based on tenant
-   `PartitionKeyTest` - Secondary partition key for routing optimization

### Reference Schemas

#### Cashier

<!--@include: ./schemas/AppDomain.Cashiers.Contracts.Models.Cashier.md#schema-->

## Technical Details

-   **Full Type:** [AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated](https://[github.url.from.config.com]/AppDomain/Cashiers/Contracts/IntegrationEvents/CashierCreated.cs)
-   **Namespace:** `AppDomain.Cashiers.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic]`
