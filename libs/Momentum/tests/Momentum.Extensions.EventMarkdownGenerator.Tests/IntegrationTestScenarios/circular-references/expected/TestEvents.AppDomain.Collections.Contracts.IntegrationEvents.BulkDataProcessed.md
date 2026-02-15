---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# BulkDataProcessed

- **Status:** Active
- **Version:** v1
- **Entity:** `bulk-data-processed`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.bulk-data-processeds.v1`
- **Estimated Payload Size:** 399 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId
## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| ProcessedFiles| `String[]` | ✓| 51 bytes (Collection size estimated (no Range constraint)) | No description available |
| RecordCounts| `List<int>` | ✓| 51 bytes (Collection size estimated (no Range constraint)) | No description available |
| ErrorMessages| `IEnumerable<string>` | ✓| 51 bytes (Collection size estimated (no Range constraint)) | No description available |
| Amounts| `ICollection<decimal>` | ✓| 171 bytes (Collection size estimated (no Range constraint)) | No description available |
| Metadata| `Dictionary<string, string>` | ✓| 51 bytes (Collection size estimated (no Range constraint)) | No description available |
| CompletedAt| `DateTime` | ✓| 8 bytes | No description available |


### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available
    
### Reference Schemas

#### Strings

<!--@include: @/events/schemas/#schema-->

#### Int32s

<!--@include: @/events/schemas/#schema-->

#### Strings

<!--@include: @/events/schemas/#schema-->

#### Decimals

<!--@include: @/events/schemas/#schema-->

#### Strings

<!--@include: @/events/schemas/#schema-->

## Technical Details

- **Full Type:** [TestEvents.AppDomain.Collections.Contracts.IntegrationEvents.BulkDataProcessed](#)
- **Namespace:** `TestEvents.AppDomain.Collections.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
