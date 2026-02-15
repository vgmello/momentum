---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->
# CashierDeleted

- **Status:** Active
- **Version:** v1
- **Entity:** `cashier`
- **Type:** Integration Event
- **Topic:** `{env}.appdomain.public.cashiers.v1`
- **Estimated Payload Size:** 40 bytes
## Description

Published when a cashier is successfully deleted from the AppDomain system.
This event contains the deleted cashier identifier and partition key information for proper message routing.

## When It's Triggered

This event is published when:
- The cashier deletion process completes successfully
- The cashier has been removed from the database
- All related cleanup operations are complete

## Event Usage

This event can be used by other services to:
- Remove cashier from operational systems
- Archive historical transaction data
- Clean up related authentication records
- Notify dependent services of cashier removal
## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant |
| CashierId| `Guid` | ✓| 16 bytes | Unique identifier of the deleted cashier |
| DeletedAt| `DateTime` | ✓| 8 bytes | Date and time when the cashier was deleted (UTC) |


## Technical Details

- **Full Type:** [AppDomain.Cashiers.Contracts.IntegrationEvents.CashierDeleted](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Cashiers/Contracts/IntegrationEvents/CashierDeleted.cs)
- **Namespace:** `AppDomain.Cashiers.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
