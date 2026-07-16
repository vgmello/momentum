---
editLink: false
---

<!-- Scenario-local override of event.liquid: dumps every single EventViewModel field verbatim,
     instead of the polished default rendering, so the raw data reaching the template is fully
     visible and reviewable in one place. -->

# Raw EventViewModel Dump: AllPropertiesShowcaseEvent

## Scalars

- **EventName:** AllPropertiesShowcaseEvent
- **EventNameKebab:** all-properties-showcase-event
- **EventTypeName:** AllPropertiesShowcaseEvent
- **FullTypeName:** TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.AllPropertiesShowcaseEvent
- **Namespace:** TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents
- **Topic:** comprehensive-topic
- **FullyQualifiedTopicName:** {env}.comprehensive-domain.internal.comprehensive-topic.v9
- **Domain:** comprehensive-domain
- **Version:** v9
- **Status:** Deprecated
- **Entity:** showcase-location
- **IsObsolete:** true
- **IsInternal:** true
- **ObsoleteMessage:** Superseded by a hypothetical future event; kept only as a documentation-generator test fixture.
- **GithubUrl:** #
- **TopicAttributeDisplayName:** [EventTopic<ShowcaseLocation>]
- **Description:** Exercises every EventMetadata property in a single event: explicit domain/topic/version,
internal visibility, an obsolete marker, multiple partition keys, a complex nested property,
and a collection of a complex type.
- **Summary:** Exercises every EventMetadata property in a single event: explicit domain/topic/version,
internal visibility, an obsolete marker, multiple partition keys, a complex nested property,
and a collection of a complex type.
- **Remarks:** ## When It's Triggered

This event exists purely as a documentation-generator test fixture, showcasing every
renderable field of the generated markdown at once.
- **Example:** await bus.PublishAsync(new AllPropertiesShowcaseEvent(tenantId, 1, primaryLocation, relatedLocations));
- **TotalEstimatedSizeBytes:** 372
- **HasInaccurateEstimates:** true

## AttributeProperties (5)

- `ShouldPluralizeTopicName` = `False`
- `Topic` = `comprehensive-topic`
- `Domain` = `comprehensive-domain`
- `Version` = `v9`
- `Internal` = `True`


## Properties (4)

### TenantId

- **Name:** TenantId
- **TypeName:** Guid
- **IsRequired:** true
- **IsComplexType:** false
- **IsCollectionType:** false
- **Description:** Identifier of the tenant that owns this record (partition key)
- **SchemaLink:** 
- **SchemaPath:** 
- **ElementTypeName:** 
- **ElementSchemaPath:** 
- **EstimatedSizeBytes:** 16
- **IsAccurate:** true
- **SizeWarning:** 
- **EstimatedSizeDisplay:** 16 bytes

### SequenceNumber

- **Name:** SequenceNumber
- **TypeName:** int
- **IsRequired:** true
- **IsComplexType:** false
- **IsCollectionType:** false
- **Description:** Secondary partition key used for ordering within a tenant (partition key)
- **SchemaLink:** 
- **SchemaPath:** 
- **ElementTypeName:** 
- **ElementSchemaPath:** 
- **EstimatedSizeBytes:** 4
- **IsAccurate:** true
- **SizeWarning:** 
- **EstimatedSizeDisplay:** 4 bytes

### PrimaryLocation

- **Name:** PrimaryLocation
- **TypeName:** ShowcaseLocation
- **IsRequired:** true
- **IsComplexType:** true
- **IsCollectionType:** false
- **Description:** The primary complex-type property, rendered with its own reference schema
- **SchemaLink:** TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md
- **SchemaPath:** TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md
- **ElementTypeName:** 
- **ElementSchemaPath:** TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md
- **EstimatedSizeBytes:** 31
- **IsAccurate:** false
- **SizeWarning:** Name: Dynamic size - no MaxLength constraint
- **EstimatedSizeDisplay:** 31 bytes (Name: Dynamic size - no MaxLength constraint)

### RelatedLocations

- **Name:** RelatedLocations
- **TypeName:** List<ShowcaseLocation>
- **IsRequired:** true
- **IsComplexType:** true
- **IsCollectionType:** true
- **Description:** A collection of complex-type entries, rendered with an element reference schema
- **SchemaLink:** TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md
- **SchemaPath:** TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md
- **ElementTypeName:** ShowcaseLocation
- **ElementSchemaPath:** TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents.ShowcaseLocation.md
- **EstimatedSizeBytes:** 321
- **IsAccurate:** false
- **SizeWarning:** Collection size estimated (no Range constraint)
- **EstimatedSizeDisplay:** 321 bytes (Collection size estimated (no Range constraint))



## PartitionKeys (2)

### TenantId

- **Name:** TenantId
- **TypeName:** Guid
- **Description:** Identifier of the tenant that owns this record
- **Order:** 0

### SequenceNumber

- **Name:** SequenceNumber
- **TypeName:** int
- **Description:** Secondary partition key used for ordering within a tenant
- **Order:** 1


