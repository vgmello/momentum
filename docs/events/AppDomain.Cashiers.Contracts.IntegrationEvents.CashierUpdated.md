---
editLink: false
---

# CashierUpdated

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `cashier`
-   **Type:** Integration Event
-   **Topic:** `{env}.AppDomain.external.cashiers.v1`
-   **Estimated Payload Size:** 32 bytes
-   **Partition Keys**: TenantId

## Description

Published when an existing cashier's information is successfully updated in the AppDomain system. This event signals that cashier data
has been modified and dependent systems should refresh their local copies.

## When It's Triggered

This event is published when: - Cashier profile information is modified (name, email, permissions, etc.) - Cashier status changes (active/inactive, role updates) - Configuration settings for the cashier are updated - The update operation completes successfully

## Data Synchronization

This event ensures: - All systems maintain consistent cashier information - Cached data is invalidated and refreshed - Downstream services can react to cashier changes appropriately - Audit trails are maintained for compliance purposes

## Event Payload

| Property  | Type   | Required | Size     | Description                                                                       |
| --------- | ------ | -------- | -------- | --------------------------------------------------------------------------------- |
| TenantId  | `Guid` | ✓        | 16 bytes | The unique identifier of the tenant that owns the updated cashier (partition key) |
| CashierId | `Guid` | ✓        | 16 bytes | The unique identifier of the cashier that was updated                             |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

## Technical Details

-   **Full Type:** [AppDomain.Cashiers.Contracts.IntegrationEvents.CashierUpdated](https://[github.url.from.config.com]/AppDomain/Cashiers/Contracts/IntegrationEvents/CashierUpdated.cs)
-   **Namespace:** `AppDomain.Cashiers.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic]`
