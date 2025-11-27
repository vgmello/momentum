---
title: Messaging
description: Event-driven messaging architecture with Wolverine and Kafka integration, supporting tenant-aware domain events and integration events for scalable microservices communication.
date: 2024-01-15
---

# Messaging

Event-driven messaging architecture with Wolverine and Kafka integration, supporting tenant-aware domain events and integration events for scalable microservices communication.

## Overview

Momentum's messaging system is built on **Wolverine** as the primary message bus, providing:

- **Domain Events**: Internal bounded context events for business process coordination
- **Integration Events**: Cross-service communication with CloudEvents standard
- **Kafka Integration**: Reliable, scalable message streaming with tenant awareness
- **Orleans Integration**: Stateful processing for complex business workflows
- **Multi-Tenant Messaging**: Tenant-scoped event publishing and handling

## Core Architecture

### Wolverine Message Bus

Wolverine serves as the core messaging infrastructure, replacing traditional MediatR patterns:

- **Command/Query Handling**: CQRS pattern implementation with automatic handler discovery
- **Event Publishing**: Both domain and integration event publishing
- **Transactional Messaging**: Inbox/Outbox patterns with PostgreSQL persistence
- **Middleware Pipeline**: Validation, error handling, and telemetry integration

### Two-Tier Event Model

Momentum implements a two-tier event publishing pattern:

```
Business Logic → Domain Events → Integration Events → Kafka → External Services
               ↓
         Orleans Grains (Stateful Processing)
```

## Event Types and Patterns

### Domain Events

Internal events within a bounded context that coordinate business processes:

```csharp
// src/AppDomain/Orders/Contracts/DomainEvents/OrderStatusChanged.cs
public record OrderStatusChanged(
    Guid TenantId,
    Guid OrderId,
    OrderStatus OldStatus,
    OrderStatus NewStatus,
    DateTime ChangedAt
) : IDomainEvent;
```

**Characteristics:**
- Handled within the same service boundary
- Enable loose coupling between domain components
- Can trigger Orleans grain processing
- Support tenant isolation

### Integration Events

Cross-bounded context events for service-to-service communication:

```csharp
// src/AppDomain.Contracts/IntegrationEvents/OrderCreated.cs
[EventTopic("main.orders.order-created")]
public record OrderCreated(
    Guid TenantId,
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime OrderDate,
    string Status
) : IIntegrationEvent;
```

**Characteristics:**
- Published to Kafka topics with CloudEvents format
- Follow tenant-aware topic naming conventions
- Include full state for external service consumption
- Support cross-service business process orchestration

## Message Handling Patterns

### Command Handlers with Event Publishing

```csharp
public static async Task<Result<Order>> Handle(
    CreateOrderCommand command,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // Execute business logic
    var result = await ExecuteOrderCreation(command, messaging);
    if (result.IsFailure) return result;
    
    var order = result.Value;
    
    // Publish domain event (internal coordination)
    await messaging.PublishAsync(new OrderCreated(
        order.TenantId, order.OrderId, order.CustomerId
    ), cancellationToken);
    
    // Publish integration event (external notification)
    await messaging.PublishAsync(new IntegrationEvents.OrderCreated(
        order.TenantId, order.OrderId, order.CustomerId,
        order.TotalAmount, order.OrderDate, order.Status.ToString()
    ), cancellationToken);
    
    return Result.Success(order);
}
```

### Orleans Integration for Stateful Processing

Orleans grains can subscribe to domain events for complex stateful workflows:

```csharp
// OrderProcessingGrain handles domain events for stateful order processing
public class OrderProcessingGrain : Grain, IOrderProcessingGrain
{
    public async Task Handle(OrderCreated domainEvent)
    {
        // Stateful business logic
        // Coordinate with inventory systems
        // Manage order fulfillment workflow
    }
}
```

## Tenant-Aware Messaging

### Partition Key Strategy

All events include tenant-aware partitioning for scalability and isolation:

```csharp
[EventTopic("main.orders.order-created")]
public record OrderCreated(...) : IIntegrationEvent
{
    // Automatic partition key: TenantId ensures tenant isolation
    public string GetPartitionKey() => TenantId.ToString();
}
```

### Topic Naming Convention

Topics follow a hierarchical naming pattern:
- Format: `{domain}.{subdomain}.{event-type}`
- Example: `main.orders.order-created`
- Benefits: Clear routing, filtering, and security boundaries

## Configuration and Setup

### Basic Wolverine Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Wolverine with Momentum defaults
builder.AddWolverine(opts =>
{
    // Configure Kafka integration for integration events
    opts.PublishMessage<OrderCreated>()
        .ToKafkaTopic("main.orders.order-created")
        .UseDurableOutbox();
        
    // Configure local queues for domain events
    opts.LocalQueue("domain-events")
        .UseDurableInbox()
        .ProcessInline();
});
```

### Multi-Tenant Event Discovery

Momentum automatically discovers integration events within domain assemblies:

- **Assembly Scanning**: Identifies types in `.IntegrationEvents` namespaces
- **Handler Association**: Only includes events with corresponding handlers
- **Domain Scoping**: Limits discovery to same domain boundaries
- **Compile-Time Safety**: Ensures event-handler relationships are valid

## Performance and Reliability

### Message Persistence

- **Durable Inbox/Outbox**: PostgreSQL-backed message persistence
- **Transactional Consistency**: Messages participate in database transactions
- **Delivery Guarantees**: At-least-once delivery with idempotency support

### Scalability Patterns

- **Partition-Based Scaling**: Tenant-aware partitioning for horizontal scaling
- **Orleans Clustering**: Stateful processing across multiple nodes
- **Connection Pooling**: Efficient Kafka connection management
- **Batch Processing**: Configurable batching for high-throughput scenarios

### Error Handling and Monitoring

- **Dead Letter Queues**: Failed message routing and analysis
- **Retry Policies**: Configurable exponential backoff strategies
- **Circuit Breakers**: Protection against cascading failures
- **OpenTelemetry Integration**: Distributed tracing and performance metrics

## Security Considerations

### Tenant Isolation

- **Data Segregation**: All events include tenant context
- **Access Control**: Topic-level security boundaries
- **Audit Trails**: Complete message flow tracking

### Transport Security

- **TLS Encryption**: End-to-end encryption in transit
- **SASL Authentication**: Secure authentication mechanisms
- **Message Signing**: Optional CloudEvents signature validation

## Development Patterns

### Event Handler Discovery

Wolverine automatically discovers handlers using naming conventions:

```csharp
public static class OrderEventHandlers
{
    // Automatically discovered and registered
    public static async Task Handle(
        OrderCreated domainEvent,
        IOrderNotificationService notifications)
    {
        await notifications.NotifyOrderCreated(domainEvent);
    }
    
    public static async Task Consume(
        IntegrationEvents.PaymentCompleted integrationEvent,
        IMessageBus messaging)
    {
        // Handle external integration event
        await messaging.SendAsync(new ProcessPayment(integrationEvent.OrderId));
    }
}
```

### Testing Strategies

- **In-Memory Transport**: Fast test execution without external dependencies
- **Test Containers**: Integration testing with real Kafka infrastructure
- **Event Verification**: Assert expected events are published
- **Handler Isolation**: Unit test handlers independently

## Getting Started

1. **Define Event Contracts**: Create domain and integration events with proper tenant context
2. **Implement Handlers**: Use Wolverine naming conventions for automatic discovery
3. **Configure Topics**: Set up Kafka topics with appropriate partitioning strategy
4. **Add Validation**: Integrate FluentValidation for message validation
5. **Monitor Performance**: Configure OpenTelemetry for observability
6. **Test Thoroughly**: Use both unit and integration testing approaches

## Related Topics

- [CQRS](../cqrs/index.md) - Command and query patterns with Wolverine
- [Database](../database/index.md) - Transactional messaging with PostgreSQL
- [Adding Domains](../adding-domains/index.md) - Domain-specific messaging patterns
- [Service Configuration](../service-configuration/index.md) - Wolverine and Kafka setup
- [Testing](../testing/index.md) - Testing messaging workflows