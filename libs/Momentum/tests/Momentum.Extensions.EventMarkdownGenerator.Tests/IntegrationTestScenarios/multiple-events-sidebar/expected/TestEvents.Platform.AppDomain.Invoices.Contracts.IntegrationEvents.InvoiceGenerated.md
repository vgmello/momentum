---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# InvoiceGenerated <Badge type="tip" text="Active" />

- **Domain:** TestEvents.Invoices
- **Version:** v1
- **Entity:** `InvoiceGenerated`
- **Type:** Integration Event
- **Topic:** `invoice-generated`
- **Fully Qualified Topic:** `test-events.invoices.invoice-generated.v1`
- **Estimated Payload Size:** 32 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| InvoiceId| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| Amount| `decimal` | ✓| 16 bytes | No description available |

### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available

## Technical Details

- **Full Type:** [TestEvents.Platform.AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceGenerated](#)
- **Type Name:** `InvoiceGenerated`
- **Event Slug:** `invoice-generated`
- **Namespace:** `TestEvents.Platform.AppDomain.Invoices.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<InvoiceGenerated>]`
