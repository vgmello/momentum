---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->
# InvoiceCreated

- **Status:** Active
- **Version:** v1
- **Entity:** `invoice`
- **Type:** Integration Event
- **Topic:** `{env}.appdomain.public.invoices.v1`
- **Estimated Payload Size:** 160 bytes
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
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant |
| InvoiceId| `Guid` | ✓| 16 bytes | Unique identifier for the invoice |
| [Invoice](/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md)| `Invoice` | ✓| 128 bytes | Complete invoice object containing all invoice data and configuration |



### Reference Schemas

#### Invoice

<!--@include: @/events/schemas/AppDomain.Invoices.Contracts.Models.Invoice.md#schema-->

## Technical Details

- **Full Type:** [AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceCreated](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Invoices/Contracts/IntegrationEvents/InvoiceCreated.cs)
- **Namespace:** `AppDomain.Invoices.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
