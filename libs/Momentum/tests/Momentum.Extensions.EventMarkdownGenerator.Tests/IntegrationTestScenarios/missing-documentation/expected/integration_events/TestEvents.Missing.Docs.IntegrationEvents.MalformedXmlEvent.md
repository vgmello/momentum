---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# MalformedXmlEvent <Badge type="tip" text="Active" />

- **Domain:** TestEvents
- **Version:** v1
- **Entity:** `MalformedXmlEvent`
- **Type:** Integration Event
- **Topic:** `malformed-xml-events`
- **Fully Qualified Topic:** `test-events.malformed-xml-events.v1`
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
