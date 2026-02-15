---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->
# CashierCreated

- **Status:** Active
- **Version:** v1
- **Entity:** `cashier`
- **Type:** Integration Event
- **Topic:** `{env}.appdomain.public.cashiers.v1`
- **Estimated Payload Size:** 476 bytes ⚠️ *Contains dynamic properties*
## Description

Published when a new cashier is successfully created in the AppDomain system. This event contains the complete cashier data and
partition
key information for proper message routing.

## When It's Triggered

This event is published when:
- The cashier creation process completes successfully
- All validation rules pass for the new cashier data
- The cashier has been persisted to the database

## Event Usage

This event can be used by other services to:
- Initialize cashier profiles in external systems
- Set up authentication and authorization
- Configure related business processes
- Update reporting and analytics systems
## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant |
| [Cashier](/events/schemas/AppDomain.Cashiers.Contracts.Models.Cashier.md)| `Cashier` | ✓| 460 bytes (CashierPayments: Collection size estimated (no Range constraint)) | Complete cashier object containing all cashier data and configuration |



### Reference Schemas

#### Cashier

<!--@include: @/events/schemas/AppDomain.Cashiers.Contracts.Models.Cashier.md#schema-->

## Technical Details

- **Full Type:** [AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Cashiers/Contracts/IntegrationEvents/CashierCreated.cs)
- **Namespace:** `AppDomain.Cashiers.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
