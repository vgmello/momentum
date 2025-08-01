---
editLink: false
---

# CashierDeleted

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `cashier`
-   **Type:** Integration Event
-   **Topic:** `{env}.AppDomain.external.cashiers.v1`
-   **Estimated Payload Size:** 32 bytes
-   **Partition Keys**: TenantId

## Description

Published when a cashier is successfully deleted from the AppDomain system. This event contains the essential identifiers for the deleted
cashier and ensures proper cleanup and audit trail.

## When It's Triggered

This event is published when: - A cashier is permanently removed from the system - The deletion process completes successfully - All associated cleanup operations are finished

## Impact and Considerations

When this event is published: - All dependent systems should remove references to this cashier - Any cached data related to this cashier should be invalidated - Audit logs should record the deletion for compliance purposes

## Event Payload

| Property  | Type   | Required | Size     | Description                                                                        |
| --------- | ------ | -------- | -------- | ---------------------------------------------------------------------------------- |
| TenantId  | `Guid` | ✓        | 16 bytes | The unique identifier of the tenant that owned the deleted cashier (partition key) |
| CashierId | `Guid` | ✓        | 16 bytes | The unique identifier of the cashier that was deleted                              |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

## Technical Details

-   **Full Type:** [AppDomain.Cashiers.Contracts.IntegrationEvents.CashierDeleted](https://[github.url.from.config.com]/AppDomain/Cashiers/Contracts/IntegrationEvents/CashierDeleted.cs)
-   **Namespace:** `AppDomain.Cashiers.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic]`
