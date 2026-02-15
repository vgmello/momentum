---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->
# InvoiceFinalized

- **Status:** Active
- **Version:** v1
- **Entity:** `invoice`
- **Type:** Integration Event
- **Topic:** `{env}.appdomain.public.invoices.v1`
- **Estimated Payload Size:** 68 bytes
## Description

Published when an invoice is finalized and ready for processing in the AppDomain system.
This event contains the essential invoice information for external systems.

## When It's Triggered

This event is published when:
- An invoice completes the finalization process
- All invoice line items and calculations are confirmed
- Invoice is ready for customer delivery or payment collection

## Event Usage

This event can be used by other services to:
- Generate invoice documents for customer delivery
- Initialize payment collection processes
- Update customer relationship management systems
- Trigger invoice delivery workflows
## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant |
| InvoiceId| `Guid` | ✓| 16 bytes | Unique identifier for the invoice |
| CustomerId| `Guid` | ✓| 16 bytes | Unique identifier for the customer |
| PublicInvoiceNumber| `string` | ✓| 4 bytes | Public-facing invoice number for customer reference |
| FinalTotalAmount| `decimal` | ✓| 16 bytes | Final total amount of the invoice |


## Technical Details

- **Full Type:** [AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceFinalized](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Invoices/Contracts/IntegrationEvents/InvoiceFinalized.cs)
- **Namespace:** `AppDomain.Invoices.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
