---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->
# PaymentReceived

- **Status:** Active
- **Version:** v1
- **Entity:** ``
- **Type:** Integration Event
- **Topic:** `{env}.appdomain.public.payments.v1`
- **Estimated Payload Size:** 68 bytes
## Description

Published when a payment is received for an invoice in the AppDomain system.
This event contains the payment details for proper message routing and processing.

## When It's Triggered

This event is published when:
- A payment is received and processed for an invoice
- Payment validation completes successfully
- Payment details are recorded in the system

## Event Usage

This event can be used by other services to:
- Update invoice payment status
- Process partial or full payment reconciliation
- Send payment received notifications
- Update accounting and financial systems
## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant |
| InvoiceId| `Guid` | ✓| 16 bytes | Unique identifier of the invoice the payment is for |
| Currency| `string` | ✓| 4 bytes | Currency of the payment |
| PaymentAmount| `decimal` | ✓| 16 bytes | Amount of the payment received |
| PaymentDate| `DateTime` | ✓| 8 bytes | Date and time when the payment was received |
| PaymentMethod| `string` |  | 4 bytes | Method used for the payment (optional) |
| PaymentReference| `string` | ✓| 4 bytes | Unique reference or transaction ID for the payment, used for tracking and reconciliation |


## Technical Details

- **Full Type:** [AppDomain.Invoices.Contracts.IntegrationEvents.PaymentReceived](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Invoices/Contracts/IntegrationEvents/PaymentReceived.cs)
- **Namespace:** `AppDomain.Invoices.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
