---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# AllPropertiesShowcaseEvent <Badge type="danger" text="Deprecated" />

> [!CAUTION]
    > This event is deprecated. Superseded by a hypothetical future event; kept only as a documentation-generator test fixture.

- **Domain:** comprehensive-domain.Comprehensive
- **Version:** v9
- **Entity:** `ShowcaseLocation`
- **Type:** Domain Event
- **Topic:** `comprehensive-topic`
- **Fully Qualified Topic:** `internal.comprehensive-domain.comprehensive.comprehensive-topic.v9`
- **Estimated Payload Size:** 372 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId, SequenceNumber

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| SequenceNumber| `int` | ✓| 4 bytes | No description available (partition key) |
| [PrimaryLocation](/events/schemas/TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md)| `ShowcaseLocation` | ✓| 31 bytes (Name: Dynamic size - no MaxLength constraint) | No description available |
| [RelatedLocations](/events/schemas/TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md)| `List<ShowcaseLocation>` | ✓| 321 bytes (Collection size estimated (no Range constraint)) | No description available |


### Partition Keys

This event uses multiple partition keys for message routing:
- `TenantId` - No description available
    - `SequenceNumber` - No description available
    
### Reference Schemas

#### ShowcaseLocation

<!--@include: @/events/schemas/TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md#schema-->

#### ShowcaseLocations

<!--@include: @/events/schemas/TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md#schema-->

## Technical Details

- **Full Type:** [TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.AllPropertiesShowcaseEvent](#)
- **Type Name:** `AllPropertiesShowcaseEvent`
- **Event Slug:** `all-properties-showcase-event`
- **Namespace:** `TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<ShowcaseLocation>]`
