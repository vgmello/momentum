# ServiceBusOptions Detailed Configuration Guide

## Overview

This class configures the core messaging infrastructure used for CQRS patterns, event-driven architecture, and cross-service communication. It integrates with PostgreSQL for message persistence and supports CloudEvents for standardized messaging.

The configuration automatically derives service names and URNs from the application assembly name, following domain-driven design patterns where the assembly name reflects the business domain.

## Domain Property

The business domain name. Defaults to the main namespace of the entry assembly. For example, if the assembly is "ECommerce.OrderService", the domain defaults to "ECommerce".

This property is used to group related services and organize message routing. It should represent the business domain rather than technical concerns.

## PublicServiceName Property

The service name in kebab-case format. If not explicitly set, defaults to the application name converted to lowercase with dots replaced by hyphens.

This name is used for:

- Message routing and topic naming
- Service discovery and registration
- CloudEvents source identification
- Database schema naming for message persistence

The name should be stable across deployments and follow DNS naming conventions (lowercase, hyphens, no underscores).

### Examples of good service names:

- order-service
- payment-processor
- inventory-manager
- customer-portal

## ServiceUrn Property

A relative URI in the format "/{domain_snake_case}/{service_name}" that uniquely identifies this service within the messaging infrastructure.

This URN is automatically generated during configuration and used for:

- Message routing and subscription patterns
- Service identification in distributed tracing
- Dead letter queue naming
- Health check endpoint registration

The URN format ensures uniqueness across different domains and services while maintaining readability and following URI conventions.

### Example URNs generated from configuration:

```
// Domain: "ECommerce", PublicServiceName: "order-service"
// Generated URN: "/e_commerce/order-service"

// Domain: "CustomerManagement", PublicServiceName: "customer-api"  
// Generated URN: "/customer_management/customer-api"
```

## CloudEvents Configuration

CloudEvents provides a standardized format for event data, enabling interoperability between different services and platforms. This configuration controls how events are formatted when published to external systems.

### Key CloudEvents properties configured:

- **Source:** URI identifying the event producer
- **Type:** Event type for categorization and routing
- **Subject:** Subject of the event for filtering
- **DataContentType:** Format of the event data

### CloudEvents Configuration Example:

```json
{
  "ServiceBus": {
    "CloudEvents": {
      "Source": "https://api.mystore.com/orders",
      "DefaultType": "com.mystore.orders",
      "Subject": "orders",
      "DataContentType": "application/json"
    }
  }
}
```

### Generated CloudEvent Example:

```json
{
  "specversion": "1.0",
  "type": "com.mystore.orders.order-created",
  "source": "https://api.mystore.com/orders",
  "subject": "orders/12345",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "time": "2024-01-15T10:30:00Z",
  "datacontenttype": "application/json",
  "data": {
    "orderId": "12345",
    "customerId": "67890",
    "totalAmount": 99.99
  }
}
```

## Post-Configuration Processing

The ServiceBusOptions.Configurator runs after the options have been bound from configuration and performs final setup including:

- Setting default PublicServiceName if not provided
- Generating the service URN from domain and service name
- Validating required connection strings
- Logging configuration warnings for missing dependencies

The configurator follows the .NET options pattern for post-configuration processing, ensuring all derived values are computed correctly even when base configuration is incomplete.

### Automatic Configuration Example:

```
// Given application name: "ECommerce.OrderService"
// And configuration:
{
  "ServiceBus": {
    "Domain": "ECommerce"
    // PublicServiceName not specified
  }
}

// After post-configuration:
// PublicServiceName = "ecommerce-orderservice" (derived from app name)
// ServiceUrn = "/e_commerce/ecommerce-orderservice" (generated URN)
```

### Warning Scenarios:

```
// Missing connection string warning:
// "ConnectionStrings:ServiceBus is not set. Transactional Inbox/Outbox 
//  and Message Persistence features disabled"

// This allows the application to start without messaging persistence
// but logs a clear warning about reduced functionality
```