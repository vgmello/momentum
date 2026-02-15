---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# SubscriptionCancelled

- **Status:** Active
- **Version:** v1
- **Entity:** `subscription-cancelled`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.subscription-cancelleds.v1`
- **Estimated Payload Size:** 24 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| SubscriptionId| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| Reason| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| CancelledAt| `DateTime` | ✓| 8 bytes | No description available |


### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available
    ## Technical Details

- **Full Type:** [TestEvents.Platform.Subscriptions.Contracts.IntegrationEvents.SubscriptionCancelled](#)
- **Namespace:** `TestEvents.Platform.Subscriptions.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
