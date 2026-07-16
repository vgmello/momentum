---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# MinimalEvent

- **Status:** Active
- **Domain:** TestEvents
- **Version:** v1
- **Entity:** `minimal-event`
- **Type:** Integration Event
- **Topic:** `minimal-events`
- **Fully Qualified Topic:** `test-events.public.minimal-events.v1`
- **Estimated Payload Size:** 16 bytes
- **Partition Keys**: Id

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| Id| `Guid` | ✓| 16 bytes | No description available (partition key) |


### Partition Keys

This event uses a partition key for message routing:
- `Id` - No description available
    ## Technical Details

- **Full Type:** [TestEvents.Events.Core.IntegrationEvents.MinimalEvent](#)
- **Type Name:** `MinimalEvent`
- **Event Slug:** `minimal-event`
- **Namespace:** `TestEvents.Events.Core.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<MinimalEvent>]`
- **Attribute Properties:**
- `ShouldPluralizeTopicName`: `True`
- `Topic`: `minimal-event`
- `Domain`: *(empty)*
- `Version`: `v1`
- `Internal`: `False`
