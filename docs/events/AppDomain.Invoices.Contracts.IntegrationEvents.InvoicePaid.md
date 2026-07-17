---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# InvoicePaid <Badge type="tip" text="Active" />

- **Domain:** AppDomain.Invoices
- **Version:** v1
- **Entity:** [Invoice](/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md)
- **Type:** Integration Event
- **Topic:** `invoices`
- **Fully Qualified Topic:** `app-domain.invoices.invoices.v1`
- **Estimated Payload Size:** 318 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId, InvoiceId

## Description

Published when an invoice is successfully marked as paid in the AppDomain system.
This event contains the updated invoice data with payment information for proper message routing.

## When It's Triggered

This event is published when:
- An invoice payment is successfully recorded
- Payment validation completes successfully
- Invoice status is updated to paid in the database

## Event Usage

This event can be used by other services to:
- Update customer account balances
- Trigger revenue recognition processes
- Send payment confirmation notifications
- Update financial reporting systems
## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant (partition key) |
| InvoiceId| `Guid` | ✓| 16 bytes | Unique identifier for the invoice (partition key) |
| [Invoice](/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md)| `Invoice` | ✓| 286 bytes (Name: Dynamic size - no MaxLength constraint, Currency: Dynamic size - no MaxLength constraint) | Updated invoice object with payment information |

### Partition Keys

This event uses multiple partition keys for message routing:
- `TenantId` - Unique identifier for the tenant
- `InvoiceId` - Unique identifier for the invoice

### Reference Schemas

#### Invoice

<!--@include: @/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md#schema-->

## Technical Details

- **Full Type:** [AppDomain.Invoices.Contracts.IntegrationEvents.InvoicePaid](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Invoices/Contracts/IntegrationEvents/InvoicePaid.cs)
- **Type Name:** `InvoicePaid`
- **Event Slug:** `invoice-paid`
- **Namespace:** `AppDomain.Invoices.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<Invoice>]`
