---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# UserDeleted

- **Status:** Active
- **Version:** v1
- **Entity:** `user-deleted`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.user-deleteds.v1`
- **Estimated Payload Size:** 24 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| UserId| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| DeletedAt| `DateTime` | ✓| 8 bytes | No description available |


### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available
    ## Technical Details

- **Full Type:** [TestEvents.Platform.Users.Contracts.IntegrationEvents.UserDeleted](#)
- **Namespace:** `TestEvents.Platform.Users.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
