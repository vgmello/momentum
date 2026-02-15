---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# MinimalEvent

- **Status:** Active
- **Version:** v1
- **Entity:** `minimal-event`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.minimal-events.v1`
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
- **Namespace:** `TestEvents.Events.Core.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
