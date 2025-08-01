---
editLink: false
---

# InvoiceCancelled

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `invoice`
-   **Type:** Integration Event
-   **Topic:** `{env}.AppDomain.external.invoices.v1`
-   **Estimated Payload Size:** 32 bytes
-   **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property  | Type   | Required | Size     | Description                              |
| --------- | ------ | -------- | -------- | ---------------------------------------- |
| TenantId  | `Guid` | ✓        | 16 bytes | No description available (partition key) |
| InvoiceId | `Guid` | ✓        | 16 bytes | No description available                 |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

## Technical Details

-   **Full Type:** [AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceCancelled](https://[github.url.from.config.com]/AppDomain/Invoices/Contracts/IntegrationEvents/InvoiceCancelled.cs)
-   **Namespace:** `AppDomain.Invoices.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic]`
