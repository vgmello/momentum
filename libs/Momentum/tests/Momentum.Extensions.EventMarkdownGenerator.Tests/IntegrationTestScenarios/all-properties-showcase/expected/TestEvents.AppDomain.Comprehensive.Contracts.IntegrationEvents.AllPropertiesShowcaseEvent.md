---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# AllPropertiesShowcaseEvent

> [!CAUTION]
    > This event is deprecated. Superseded by a hypothetical future event; kept only as a documentation-generator test fixture.

- **Status:** Deprecated
- **Domain:** comprehensive-domain
- **Version:** v9
- **Entity:** `showcase-location`
- **Type:** Domain Event
- **Topic:** `comprehensive-topic`
- **Fully Qualified Topic:** `{env}.testevents.internal.comprehensive-topic.v9`
- **Estimated Payload Size:** 372 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId, SequenceNumber

## Description

Exercises every EventMetadata property in a single event: explicit domain/topic/version,
internal visibility, an obsolete marker, multiple partition keys, a complex nested property,
and a collection of a complex type.

## When It's Triggered

This event exists purely as a documentation-generator test fixture, showcasing every
renderable field of the generated markdown at once.
### Example

await bus.PublishAsync(new AllPropertiesShowcaseEvent(tenantId, 1, primaryLocation, relatedLocations));
## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | Identifier of the tenant that owns this record (partition key) |
| SequenceNumber| `int` | ✓| 4 bytes | Secondary partition key used for ordering within a tenant (partition key) |
| [PrimaryLocation](/events/schemas/TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md)| `ShowcaseLocation` | ✓| 31 bytes (Name: Dynamic size - no MaxLength constraint) | The primary complex-type property, rendered with its own reference schema |
| [RelatedLocations](/events/schemas/TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md)| `List<ShowcaseLocation>` | ✓| 321 bytes (Collection size estimated (no Range constraint)) | A collection of complex-type entries, rendered with an element reference schema |


### Partition Keys

This event uses multiple partition keys for message routing:
- `TenantId` - Identifier of the tenant that owns this record
    - `SequenceNumber` - Secondary partition key used for ordering within a tenant
    
### Reference Schemas

#### ShowcaseLocation

<!--@include: @/events/schemas/TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md#schema-->

#### ShowcaseLocations

<!--@include: @/events/schemas/TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md#schema-->

## Technical Details

- **Full Type:** [TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.AllPropertiesShowcaseEvent](#)
- **Type Name:** `AllPropertiesShowcaseEvent`
- **Namespace:** `TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<ShowcaseLocation>]`
- **Attribute Properties:**
- `ShouldPluralizeTopicName`: `False`
- `Topic`: `comprehensive-topic`
- `Domain`: `comprehensive-domain`
- `Version`: `v9`
- `Internal`: `True`
