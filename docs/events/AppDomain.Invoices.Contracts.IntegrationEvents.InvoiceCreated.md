---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# InvoiceCreated <Badge type="tip" text="Active" />

- **Domain:** AppDomain.Invoices
- **Version:** v1
- **Entity:** [Invoice](/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md)
- **Type:** Integration Event
- **Topic:** `invoices`
- **Fully Qualified Topic:** `app-domain.invoices.invoices.v1`
- **Estimated Payload Size:** 318 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId, InvoiceId

## Description

Published when a new invoice is successfully created in the AppDomain system.
This event contains the complete invoice data and partition key information for proper message routing.

## When It's Triggered

This event is published when:
- The invoice creation process completes successfully
- All validation rules pass
- The invoice data has been persisted to the database

## Event Usage

This event can be used by other services to:
- Update accounting systems
- Trigger app_domain workflows
- Send notifications to relevant stakeholders
- Update customer portals with new invoice information
## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant (partition key) |
| InvoiceId| `Guid` | ✓| 16 bytes | Unique identifier for the invoice (partition key) |
| [Invoice](/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md)| `Invoice` | ✓| 286 bytes (Name: Dynamic size - no MaxLength constraint, Currency: Dynamic size - no MaxLength constraint) | Complete invoice object containing all invoice data and configuration |


### Partition Keys

This event uses multiple partition keys for message routing:
- `TenantId` - Unique identifier for the tenant
    - `InvoiceId` - Unique identifier for the invoice
    
### Reference Schemas

#### Invoice

<!--@include: @/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md#schema-->

## Technical Details

- **Full Type:** [AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceCreated](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Invoices/Contracts/IntegrationEvents/InvoiceCreated.cs)
- **Type Name:** `InvoiceCreated`
- **Event Slug:** `invoice-created`
- **Namespace:** `AppDomain.Invoices.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<Invoice>]`
