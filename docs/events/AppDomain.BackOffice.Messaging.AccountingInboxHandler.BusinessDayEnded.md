---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# BusinessDayEnded

- **Status:** Active
- **Version:** v1
- **Entity:** ``
- **Type:** Integration Event
- **Topic:** `{env}.appdomain.public.momentum.v1`
- **Estimated Payload Size:** 8 bytes ⚠️ *Contains dynamic properties*

## Description

Represents an event indicating that a business day has ended for a specific market and region.

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| BusinessDate| `DateTime` | ✓| 8 bytes | The date of the business day that ended. |
| Market| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | The market identifier where the business day ended. |
| Region| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | The region identifier where the business day ended. |


## Technical Details

- **Full Type:** [AppDomain.BackOffice.Messaging.AccountingInboxHandler.BusinessDayEnded](https://github.com/vgmello/momentum/blob/main/src/AppDomain/BackOffice/Messaging/AccountingInboxHandler/BusinessDayEnded.cs)
- **Namespace:** `AppDomain.BackOffice.Messaging.AccountingInboxHandler`
- **Topic Attribute:** `[EventTopic]`
