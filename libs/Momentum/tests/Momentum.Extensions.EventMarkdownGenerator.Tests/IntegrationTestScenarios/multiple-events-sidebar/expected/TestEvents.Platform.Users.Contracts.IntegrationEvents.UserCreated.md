---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# UserCreated <Badge type="tip" text="Active" />

- **Domain:** TestEvents.Users
- **Version:** v1
- **Entity:** `UserCreated`
- **Type:** Integration Event
- **Topic:** `user-created`
- **Fully Qualified Topic:** `test-events.users.user-created.v1`
- **Estimated Payload Size:** 16 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| UserId| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| Email| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |

### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available

## Technical Details

- **Full Type:** [TestEvents.Platform.Users.Contracts.IntegrationEvents.UserCreated](#)
- **Type Name:** `UserCreated`
- **Event Slug:** `user-created`
- **Namespace:** `TestEvents.Platform.Users.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<UserCreated>]`
