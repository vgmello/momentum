---
editLink: false
---

# BulkDataProcessed

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `bulk`
-   **Type:** Integration Event
-   **Topic:** `{env}.AppDomain.public.collections.v1`
-   **Partition Keys**: TenantId

## Description

Published when bulk data processing operations complete with mixed result types

## When It's Triggered

This event is published when:

-   Batch processing jobs complete
-   Multiple data types are processed together
-   Collection-based operations finish

## Collection Types

This event demonstrates various collection patterns:

-   Arrays of primitive types
-   Lists of complex objects
-   Generic collections with different element types
-   Nested collections within complex types

## Event Payload

| Property       | Type                         | Required | Description                                                           |
| -------------- | ---------------------------- | -------- | --------------------------------------------------------------------- |
| TenantId       | `Guid`                       | ✓        | Identifier of the tenant that owns the bulk operation (partition key) |
| ProcessedFiles | `string[]`                   | ✓        | Array of file names that were processed during the operation          |
| RecordCounts   | `List<int>`                  | ✓        | List of record counts for each processed batch                        |
| ErrorMessages  | `IEnumerable<string>`        | ✓        | Enumerable collection of error messages encountered during processing |
| Amounts        | `ICollection<decimal>`       | ✓        | Collection of monetary amounts processed in the operation             |
| Metadata       | `Dictionary<string, string>` | ✓        | Dictionary of key-value pairs containing operation metadata           |
| CompletedAt    | `DateTime`                   | ✓        | Timestamp when the bulk processing operation completed                |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

## Technical Details

-   **Full Type:** [`AppDomain.Collections.Contracts.IntegrationEvents.BulkDataProcessed`](https://[github.url.from.config.com]/AppDomain/Collections/Contracts/IntegrationEvents/BulkDataProcessed.cs)
-   **Namespace:** `AppDomain.Collections.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic<Bulk>]`
