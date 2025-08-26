# AppDomain Events Documentation

This document provides a comprehensive overview of all events published by the AppDomain domain, organized by feature and event type.

## Integration Events

Integration events are published to external systems and can be consumed by other bounded contexts and services.

<!--@include: ./integration_events.md-->

## Domain Events

<!--@include: ./domain_events.md-->

## Event Versioning Strategy

All events currently use version `v1` as specified in their `EventTopicAttribute`. When introducing breaking changes to event schemas:

1. Create a new version (e.g., `v2`) using the version parameter in `EventTopicAttribute`
2. Maintain backward compatibility by continuing to publish the previous version
3. Document migration notes in the individual event detail pages
4. Update this index to reflect version status (Active/Deprecated/Planned)

## Topic Naming Convention

Events use the following topic naming patterns:

-   **Entity-based topics**: Generated from entity names (e.g., `invoices`, `cashiers`)
-   **Custom topics**: Explicitly defined (e.g., `payment-received`)
-   **Full topic format**: `{environment}.{domain}.{visibility}.{topic}.{version}`

Where:

-   `domain` = `AppDomain`
-   `visibility` = `public` for integration events, `internal` for domain events
-   `version` = `v1` (default for all current events)
