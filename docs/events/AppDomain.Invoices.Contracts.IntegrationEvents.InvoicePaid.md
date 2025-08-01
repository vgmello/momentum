---
editLink: false
---

# InvoicePaid

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `invoice`
-   **Type:** Integration Event
-   **Topic:** `{env}.AppDomain.external.invoices.v1`
-   **Estimated Payload Size:** 132 bytes ⚠️ _Contains dynamic properties_
-   **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property                                                            | Type      | Required | Size                                                                                                                                                       | Description                              |
| ------------------------------------------------------------------- | --------- | -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------- |
| TenantId                                                            | `Guid`    | ✓        | 16 bytes                                                                                                                                                   | No description available (partition key) |
| [Invoice](./schemas/AppDomain.Invoices.Contracts.Models.Invoice.md) | `Invoice` | ✓        | 116 bytes (Name: Dynamic size - no MaxLength constraint, Status: Dynamic size - no MaxLength constraint, Currency: Dynamic size - no MaxLength constraint) | No description available                 |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

### Reference Schemas

#### Invoice

<!--@include: ./schemas/AppDomain.Invoices.Contracts.Models.Invoice.md#schema-->

## Technical Details

-   **Full Type:** [AppDomain.Invoices.Contracts.IntegrationEvents.InvoicePaid](https://[github.url.from.config.com]/AppDomain/Invoices/Contracts/IntegrationEvents/InvoicePaid.cs)
-   **Namespace:** `AppDomain.Invoices.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic]`
