---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# SubscriptionActivated

- **Status:** Active
- **Version:** v1
- **Entity:** `subscription-activated`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.subscription-activateds.v1`
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
- **Namespace:** `TestEvents.Platform.Subscriptions.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
