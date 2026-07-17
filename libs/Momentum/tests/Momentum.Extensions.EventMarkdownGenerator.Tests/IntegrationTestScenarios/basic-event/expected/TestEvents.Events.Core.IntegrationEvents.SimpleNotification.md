---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# SimpleNotification <Badge type="tip" text="Active" />

- **Domain:** TestEvents
- **Version:** v1
- **Entity:** `SimpleNotification`
- **Type:** Integration Event
- **Topic:** `simple-notifications`
- **Fully Qualified Topic:** `test-events.simple-notifications.v1`
- **Estimated Payload Size:** 16 bytes ⚠️ *Contains dynamic properties*

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| NotificationId| `Guid` | ✓| 16 bytes | No description available |
| Message| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |


## Technical Details

- **Full Type:** [TestEvents.Events.Core.IntegrationEvents.SimpleNotification](#)
- **Type Name:** `SimpleNotification`
- **Event Slug:** `simple-notification`
- **Namespace:** `TestEvents.Events.Core.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<SimpleNotification>]`
