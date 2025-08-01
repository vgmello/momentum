---
editLink: false
---

# InvoiceGenerated

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `invoice`
-   **Type:** Domain Event
-   **Topic:** `{env}.AppDomain.internal.invoices.v1`
-   **Estimated Payload Size:** 48 bytes
-   **Partition Keys**: TenantId

## Description

Published when an invoice is generated in the system.

            This event is triggered during the invoice creation process
            and contains the essential invoice information needed for
            downstream processing.

            Key details:
            - Contains tenant isolation data
            - Includes invoice identification
            - Provides total amount for processing

## Event Payload

| Property      | Type      | Required | Size     | Description                              |
| ------------- | --------- | -------- | -------- | ---------------------------------------- |
| TenantId      | `Guid`    | ✓        | 16 bytes | No description available (partition key) |
| InvoiceId     | `Guid`    | ✓        | 16 bytes | No description available                 |
| InvoiceAmount | `decimal` | ✓        | 16 bytes | No description available                 |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

## Technical Details

-   **Full Type:** [AppDomain.Invoices.Contracts.DomainEvents.InvoiceGenerated](https://[github.url.from.config.com]/AppDomain/Invoices/Contracts/DomainEvents/InvoiceGenerated.cs)
-   **Namespace:** `AppDomain.Invoices.Contracts.DomainEvents`
-   **Topic Attribute:** `[EventTopic]`
