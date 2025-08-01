---
editLink: false
---

# SubscriptionCancelled

- **Status:** Active
- **Version:** v1
- **Entity:** `subscription`
- **Type:** Integration Event
- **Topic:** `{env}.platform.public.subscriptions.v1`
- **Partition Keys**: TenantId

## Description

Published when a subscription is cancelled

## Event Payload

| Property | Type | Required | Description |
| ----------------------------------------------------------------- | --------- | -------- | --------------------------------------------------------------------- |
| TenantId | `Guid` | ✓ | Tenant identifier (partition key) |
| SubscriptionId | `string` | ✓ | Subscription identifier |
| Reason | `string` | ✓ | Cancellation reason |
| CancelledAt | `DateTime` | ✓ | Cancellation timestamp |

### Partition Keys

This event uses a partition key for message routing:

- `TenantId` - Primary partition key based on tenant

## Technical Details

- **Full Type:** [`Platform.Subscriptions.Contracts.IntegrationEvents.SubscriptionCancelled`](https://[github.url.from.config.com]/Platform/Subscriptions/Contracts/IntegrationEvents/SubscriptionCancelled.cs)
- **Namespace:** `Platform.Subscriptions.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<Subscription>]`