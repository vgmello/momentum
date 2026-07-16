---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# EmptySummaryEvent

- **Status:** Active
- **Domain:** TestEvents
- **Version:** v1
- **Entity:** `empty-summary-event`
- **Type:** Integration Event
- **Topic:** `empty-summary-events`
- **Fully Qualified Topic:** `{env}.test-events.public.empty-summary-events.v1`
- **Estimated Payload Size:** 16 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| Data| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |


### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available
    ## Technical Details

- **Full Type:** [TestEvents.Missing.Docs.IntegrationEvents.EmptySummaryEvent](#)
- **Type Name:** `EmptySummaryEvent`
- **Event Slug:** `empty-summary-event`
- **Namespace:** `TestEvents.Missing.Docs.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<EmptySummaryEvent>]`
- **Attribute Properties:**
- `ShouldPluralizeTopicName`: `True`
- `Topic`: `empty-summary-event`
- `Domain`: *(empty)*
- `Version`: `v1`
- `Internal`: `False`
