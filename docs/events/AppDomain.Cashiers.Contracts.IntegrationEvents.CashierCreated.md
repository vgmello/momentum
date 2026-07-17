---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# CashierCreated <Badge type="tip" text="Active" />

- **Domain:** AppDomain.Cashiers
- **Version:** v1
- **Entity:** [Cashier](/events/schemas/AppDomain.Cashiers.Contracts.Models.Cashier.md)
- **Type:** Integration Event
- **Topic:** `cashiers`
- **Fully Qualified Topic:** `app-domain.cashiers.cashiers.v1`
- **Estimated Payload Size:** 1008 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

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
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant (partition key) |
| [Cashier](/events/schemas/AppDomain.Cashiers.Contracts.Models.Cashier.md)| `Cashier` | ✓| 992 bytes (Name: Dynamic size - no MaxLength constraint, Email: Dynamic size - no MaxLength constraint, CashierPayments: Collection size estimated (no Range constraint)) | Complete cashier object containing all cashier data and configuration |

### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - Unique identifier for the tenant

### Reference Schemas

#### Cashier

<!--@include: @/events/schemas/AppDomain.Cashiers.Contracts.Models.Cashier.md#schema-->

## Technical Details

- **Full Type:** [AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Cashiers/Contracts/IntegrationEvents/CashierCreated.cs)
- **Type Name:** `CashierCreated`
- **Event Slug:** `cashier-created`
- **Namespace:** `AppDomain.Cashiers.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<Cashier>]`
