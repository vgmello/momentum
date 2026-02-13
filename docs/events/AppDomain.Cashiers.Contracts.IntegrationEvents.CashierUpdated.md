---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->
# CashierUpdated

- **Status:** Active
- **Version:** v1
- **Entity:** `cashier`
- **Type:** Integration Event
- **Topic:** `{env}.appdomain.public.cashiers.v1`
- **Estimated Payload Size:** 476 bytes ⚠️ *Contains dynamic properties*
## Description

Published when a cashier is successfully updated in the AppDomain system.
This event contains the updated cashier data and partition key information for proper message routing.

## When It's Triggered

This event is published when:
- The cashier update process completes successfully
- All validation rules pass for the updated data
- The updated cashier data has been persisted to the database

## Event Usage

This event can be used by other services to:
- Update cached cashier information
- Synchronize cashier data across systems
- Notify dependent services of cashier changes
## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant |
| [Cashier](/events/schemas/AppDomain.Cashiers.Contracts.Models.Cashier.md)| `Cashier` | ✓| 460 bytes (CashierPayments: Collection size estimated (no Range constraint)) | Updated cashier object containing all current cashier data |



### Reference Schemas

#### Cashier

<!--@include: @/events/schemas/AppDomain.Cashiers.Contracts.Models.Cashier.md#schema-->

## Technical Details

- **Full Type:** [AppDomain.Cashiers.Contracts.IntegrationEvents.CashierUpdated](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Cashiers/Contracts/IntegrationEvents/CashierUpdated.cs)
- **Namespace:** `AppDomain.Cashiers.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
