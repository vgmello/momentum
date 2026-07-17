---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# PaymentReceived <Badge type="tip" text="Active" />

- **Domain:** AppDomain.Invoices
- **Version:** v1
- **Entity:** `Payment`
- **Type:** Integration Event
- **Topic:** `payments`
- **Fully Qualified Topic:** `app-domain.invoices.payments.v1`
- **Estimated Payload Size:** 56 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId, InvoiceId

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
| TenantId| `Guid` | ✓| 16 bytes | Unique identifier for the tenant (partition key) |
| InvoiceId| `Guid` | ✓| 16 bytes | Unique identifier of the invoice the payment is for (partition key) |
| Currency| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | Currency of the payment |
| PaymentAmount| `decimal` | ✓| 16 bytes | Amount of the payment received |
| PaymentDate| `DateTime` | ✓| 8 bytes | Date and time when the payment was received |
| PaymentMethod| `string` |  | 0 bytes (Dynamic size - no MaxLength constraint) | Method used for the payment (optional) |
| PaymentReference| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | Unique reference or transaction ID for the payment, used for tracking and reconciliation |


### Partition Keys

This event uses multiple partition keys for message routing:
- `TenantId` - Unique identifier for the tenant
    - `InvoiceId` - Unique identifier of the invoice the payment is for
    ## Technical Details

- **Full Type:** [AppDomain.Invoices.Contracts.IntegrationEvents.PaymentReceived](https://github.com/vgmello/momentum/blob/main/src/AppDomain/Invoices/Contracts/IntegrationEvents/PaymentReceived.cs)
- **Type Name:** `PaymentReceived`
- **Event Slug:** `payment-received`
- **Namespace:** `AppDomain.Invoices.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
