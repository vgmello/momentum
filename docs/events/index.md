# AppDomain Events Documentation

This document provides a comprehensive overview of all events published by the AppDomain domain, organized by feature and event type.

## Integration Events

Integration events are published to external systems and can be consumed by other bounded contexts and services.

### Invoice Events

| Event Name                                 | Description                                                               | Status |
| ------------------------------------------ | ------------------------------------------------------------------------- | ------ |
| [InvoiceCreated](./invoice-created.md)     | Published when a new invoice is created in the system                     | Active |
| [InvoiceCancelled](./invoice-cancelled.md) | Published when an invoice is cancelled                                    | Active |
| [InvoiceFinalized](./invoice-finalized.md) | Published when an invoice is finalized during business day end processing | Active |
| [InvoicePaid](./invoice-paid.md)           | Published when an invoice is marked as paid                               | Active |
| [PaymentReceived](./payment-received.md)   | Published when a payment is received and processed                        | Active |

### Cashier Events

| Event Name                             | Description                                         | Status |
| -------------------------------------- | --------------------------------------------------- | ------ |
| [CashierCreated](./cashier-created.md) | Published when a new cashier is created             | Active |
| [CashierUpdated](./cashier-updated.md) | Published when an existing cashier is updated       | Active |
| [CashierDeleted](./cashier-deleted.md) | Published when a cashier is deleted from the system | Active |

## Domain Events

Domain events are internal to the AppDomain bounded context and used for coordinating domain logic within the service.

### Invoice Domain Events

| Event Name                                 | Description                                                                 | Status |
| ------------------------------------------ | --------------------------------------------------------------------------- | ------ |
| [InvoiceGenerated](./invoice-generated.md) | Internal event triggered when an invoice is generated for domain processing | Active |

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
