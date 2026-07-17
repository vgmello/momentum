---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# UserDeleted <Badge type="tip" text="Active" />

- **Domain:** TestEvents.Users
- **Version:** v1
- **Entity:** `UserDeleted`
- **Type:** Integration Event
- **Topic:** `user-deleteds`
- **Fully Qualified Topic:** `test-events.users.user-deleteds.v1`
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
- **Type Name:** `UserDeleted`
- **Event Slug:** `user-deleted`
- **Namespace:** `TestEvents.Platform.Users.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<UserDeleted>]`
