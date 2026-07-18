---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# SubscriptionActivated <Badge type="tip" text="Active" />

- **Domain:** TestEvents.Subscriptions
- **Version:** v1
- **Entity:** `SubscriptionActivated`
- **Type:** Integration Event
- **Topic:** `subscription-activated`
- **Fully Qualified Topic:** `test-events.subscriptions.subscription-activated.v1`
- **Estimated Payload Size:** 16 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| SubscriptionId| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| PlanName| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |

### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available

## Technical Details

- **Full Type:** [TestEvents.Platform.Subscriptions.Contracts.IntegrationEvents.SubscriptionActivated](#)
- **Type Name:** `SubscriptionActivated`
- **Event Slug:** `subscription-activated`
- **Namespace:** `TestEvents.Platform.Subscriptions.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<SubscriptionActivated>]`
