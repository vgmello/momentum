---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->
# InvoicePaid

- **Status:** Active
- **Version:** v1
- **Entity:** `invoice`
- **Type:** Integration Event
- **Topic:** `{env}.appdomain.public.invoices.v1`
- **Estimated Payload Size:** 160 bytes
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
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant |
| InvoiceId| `Guid` | ✓| 16 bytes | Unique identifier for the invoice |
| [Invoice](/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md)| `Invoice` | ✓| 128 bytes | Updated invoice object with payment information |



### Reference Schemas

#### Invoice

<!--@include: @/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md#schema-->

## Technical Details

- **Full Type:** [AppDomain.Invoices.Contracts.IntegrationEvents.InvoicePaid](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Invoices/Contracts/IntegrationEvents/InvoicePaid.cs)
- **Namespace:** `AppDomain.Invoices.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
