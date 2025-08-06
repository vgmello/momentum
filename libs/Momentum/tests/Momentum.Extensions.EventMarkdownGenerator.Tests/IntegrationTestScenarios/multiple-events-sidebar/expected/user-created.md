---
editLink: false
---

# UserCreated

- **Status:** Active
- **Version:** v1
- **Entity:** `user`
- **Type:** Integration Event
- **Topic:** `{env}.platform.public.users.v1`
- **Partition Keys**: TenantId

## Description

Published when a new user account is created

## Event Payload

| Property | Type | Required | Description |
| ----------------------------------------------------------------- | --------- | -------- | --------------------------------------------------------------------- |
| TenantId | `Guid` | ✓ | Tenant identifier (partition key) |
| UserId | `string` | ✓ | User identifier |
| Email | `string` | ✓ | User email address |

### Partition Keys

This event uses a partition key for message routing:

- `TenantId` - Primary partition key based on tenant

## Technical Details

- **Full Type:** [`Platform.Users.Contracts.IntegrationEvents.UserCreated`](https://[github.url.from.config.com]/Platform/Users/Contracts/IntegrationEvents/UserCreated.cs)
- **Namespace:** `Platform.Users.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<User>]`