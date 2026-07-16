---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# MalformedXmlEvent

- **Status:** Active
- **Domain:** TestEvents
- **Version:** v1
- **Entity:** `malformed-xml-event`
- **Type:** Integration Event
- **Topic:** `malformed-xml-events`
- **Fully Qualified Topic:** `{env}.test-events.public.malformed-xml-events.v1`
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

- **Full Type:** [TestEvents.Missing.Docs.IntegrationEvents.MalformedXmlEvent](#)
- **Type Name:** `MalformedXmlEvent`
- **Event Slug:** `malformed-xml-event`
- **Namespace:** `TestEvents.Missing.Docs.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<MalformedXmlEvent>]`
- **Attribute Properties:**
- `ShouldPluralizeTopicName`: `True`
- `Topic`: `malformed-xml-event`
- `Domain`: *(empty)*
- `Version`: `v1`
- `Internal`: `False`
