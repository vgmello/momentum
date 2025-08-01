---
editLink: false
---

# InternalAuditLogCreated

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `audit`
-   **Type:** Integration Event
-   **Topic:** `{env}.AppDomain.internal.audit.v1`
-   **Partition Keys**: TenantId

## Description

Published internally when audit log entries are created for compliance tracking

## When It's Triggered

This internal event is published when:

-   Sensitive operations are performed that require audit logging
-   Compliance-related actions need to be tracked
-   Internal system actions require monitoring

## Security Notice

This is an internal event that should not be exposed to external systems.
It contains sensitive audit information that must remain within the system boundary.

## Processing Requirements

-   Event must be processed within the same security context
-   Audit data must be encrypted at rest
-   Access requires elevated permissions

## Event Payload

| Property  | Type       | Required | Description                                                  |
| --------- | ---------- | -------- | ------------------------------------------------------------ |
| TenantId  | `Guid`     | ✓        | Identifier of the tenant for audit isolation (partition key) |
| UserId    | `string`   | ✓        | Identifier of the user who performed the action              |
| Action    | `string`   | ✓        | Description of the action that was performed                 |
| Resource  | `string`   | ✓        | Resource that was affected by the action                     |
| Timestamp | `DateTime` | ✓        | When the audited action occurred                             |
| Metadata  | `string`   | ✓        | Additional metadata about the action in JSON format          |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

## Technical Details

-   **Full Type:** [`AppDomain.Internal.Audit.IntegrationEvents.InternalAuditLogCreated`](https://[github.url.from.config.com]/AppDomain/Internal/Audit/IntegrationEvents/InternalAuditLogCreated.cs)
-   **Namespace:** `AppDomain.Internal.Audit.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic<Audit>]`
