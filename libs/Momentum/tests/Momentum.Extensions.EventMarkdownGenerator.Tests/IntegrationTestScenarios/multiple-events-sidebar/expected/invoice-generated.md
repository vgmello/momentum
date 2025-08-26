---
editLink: false
---

# InvoiceGenerated

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `invoice`
-   **Type:** Integration Event
-   **Topic:** `{env}.platform.public.invoices.v1`
-   **Partition Keys**: TenantId

## Description

Published when an invoice is generated for a customer

## Event Payload

| Property  | Type      | Required | Description                       |
| --------- | --------- | -------- | --------------------------------- |
| TenantId  | `Guid`    | ✓        | Tenant identifier (partition key) |
| InvoiceId | `string`  | ✓        | Invoice identifier                |
| Amount    | `decimal` | ✓        | Invoice amount                    |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

## Technical Details

-   **Full Type:** [`Platform.AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceGenerated`](https://[github.url.from.config.com]/Platform/AppDomain/Invoices/Contracts/IntegrationEvents/InvoiceGenerated.cs)
-   **Namespace:** `Platform.AppDomain.Invoices.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic<Invoice>]`
