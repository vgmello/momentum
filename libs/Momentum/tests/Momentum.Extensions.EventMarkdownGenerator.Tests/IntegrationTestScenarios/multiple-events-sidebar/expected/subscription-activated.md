---
editLink: false
---

# SubscriptionActivated

- **Status:** Active
- **Version:** v1
- **Entity:** `subscription`
- **Type:** Integration Event
- **Topic:** `{env}.platform.public.subscriptions.v1`
- **Partition Keys**: TenantId

## Description

Published when a subscription becomes active

## Event Payload

| Property | Type | Required | Description |
| ----------------------------------------------------------------- | --------- | -------- | --------------------------------------------------------------------- |
| TenantId | `Guid` | ✓ | Tenant identifier (partition key) |
| SubscriptionId | `string` | ✓ | Subscription identifier |
| PlanName | `string` | ✓ | Subscription plan name |

### Partition Keys

This event uses a partition key for message routing:

- `TenantId` - Primary partition key based on tenant

## Technical Details

- **Full Type:** [`Platform.Subscriptions.Contracts.IntegrationEvents.SubscriptionActivated`](https://[github.url.from.config.com]/Platform/Subscriptions/Contracts/IntegrationEvents/SubscriptionActivated.cs)
- **Namespace:** `Platform.Subscriptions.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<Subscription>]`