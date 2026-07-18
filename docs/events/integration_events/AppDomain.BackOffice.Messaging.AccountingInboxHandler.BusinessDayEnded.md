---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# BusinessDayEnded <Badge type="tip" text="Active" />

- **Domain:** accounting
- **Version:** v1
- **Entity:** `BusinessDay`
- **Type:** Integration Event
- **Topic:** `momentum`
- **Fully Qualified Topic:** `accounting.momentum.v1`
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

- **Full Type:** [AppDomain.BackOffice.Messaging.AccountingInboxHandler.BusinessDayEnded](#)
- **Type Name:** `BusinessDayEnded`
- **Event Slug:** `business-day-ended`
- **Namespace:** `AppDomain.BackOffice.Messaging.AccountingInboxHandler`
- **Topic Attribute:** `[EventTopic]`
