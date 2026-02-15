---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# InternalAuditLogCreated

- **Status:** Active
- **Version:** v1
- **Entity:** `audit-log`
- **Type:** Domain Event
- **Topic:** `{env}.testevents.internal.audit-logs.v1`
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
- **Namespace:** `TestEvents.AppDomain.Internal.Audit.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
