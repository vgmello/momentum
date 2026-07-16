---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# InternalAuditLogCreated <Badge type="tip" text="Active" />

- **Domain:** TestEvents
- **Version:** v1
- **Entity:** `AuditLog`
- **Type:** Domain Event
- **Topic:** `audit-logs`
- **Fully Qualified Topic:** `internal.test-events.audit-logs.v1`
- **Estimated Payload Size:** 24 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| UserId| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| Action| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| Resource| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| Timestamp| `DateTime` | ✓| 8 bytes | No description available |
| Metadata| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |


### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available
    ## Technical Details

- **Full Type:** [TestEvents.AppDomain.Internal.Audit.IntegrationEvents.InternalAuditLogCreated](#)
- **Type Name:** `InternalAuditLogCreated`
- **Event Slug:** `internal-audit-log-created`
- **Namespace:** `TestEvents.AppDomain.Internal.Audit.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<AuditLog>]`
