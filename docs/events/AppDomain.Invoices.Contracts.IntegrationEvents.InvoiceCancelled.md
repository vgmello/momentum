---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->
# InvoiceCancelled

- **Status:** Active
- **Version:** v1
- **Entity:** `invoice`
- **Type:** Integration Event
- **Topic:** `{env}.appdomain.public.invoices.v1`
- **Estimated Payload Size:** 160 bytes
## Description

Published when an invoice is successfully cancelled in the AppDomain system.
This event contains the cancelled invoice data for proper message routing.

## When It's Triggered

This event is published when:
- An invoice is successfully cancelled
- Cancellation validation passes
- Invoice status is updated to cancelled in the database

## Event Usage

This event can be used by other services to:
- Update customer account records
- Reverse any pending payment processes
- Send cancellation notifications
- Update financial reporting systems
## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant |
| InvoiceId| `Guid` | ✓| 16 bytes | Unique identifier for the invoice |
| [Invoice](/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md)| `Invoice` | ✓| 128 bytes | Cancelled invoice object with updated status |



### Reference Schemas

#### Invoice

<!--@include: @/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md#schema-->

## Technical Details

- **Full Type:** [AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceCancelled](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Invoices/Contracts/IntegrationEvents/InvoiceCancelled.cs)
- **Namespace:** `AppDomain.Invoices.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
