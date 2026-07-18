---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# InvoiceCancelled <Badge type="tip" text="Active" />

- **Domain:** AppDomain.Invoices
- **Version:** v1
- **Entity:** [Invoice](/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md)
- **Type:** Integration Event
- **Topic:** `invoices`
- **Fully Qualified Topic:** `app-domain.invoices.invoices.v1`
- **Estimated Payload Size:** 318 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId, InvoiceId

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
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant (partition key) |
| InvoiceId| `Guid` | ✓| 16 bytes | Unique identifier for the invoice (partition key) |
| [Invoice](/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md)| `Invoice` | ✓| 286 bytes (Name: Dynamic size - no MaxLength constraint, Currency: Dynamic size - no MaxLength constraint) | Cancelled invoice object with updated status |

### Partition Keys

This event uses multiple partition keys for message routing:
- `TenantId` - Unique identifier for the tenant
- `InvoiceId` - Unique identifier for the invoice

### Reference Schemas

#### Invoice

<!--@include: @/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md#schema-->

## Technical Details

- **Full Type:** [AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceCancelled](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Invoices/Contracts/IntegrationEvents/InvoiceCancelled.cs)
- **Type Name:** `InvoiceCancelled`
- **Event Slug:** `invoice-cancelled`
- **Namespace:** `AppDomain.Invoices.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<Invoice>]`
