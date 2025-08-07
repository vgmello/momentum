# Momentum.Extensions.Messaging.Kafka

Kafka messaging integration package for the Momentum platform, providing event-driven architecture capabilities with CloudEvents support and automatic topic management.

## Overview

This package extends the Momentum platform with Apache Kafka messaging capabilities, enabling reliable event-driven communication between microservices. It builds on top of `Momentum.ServiceDefaults` to provide seamless integration with the platform's observability, health checks, and configuration systems.

## Installation

Add the package to your project using the .NET CLI:

```bash
dotnet add package Momentum.Extensions.Messaging.Kafka
```

Or using the Package Manager Console:

```powershell
Install-Package Momentum.Extensions.Messaging.Kafka
```

## Key Features

- **Event-Driven Architecture**: Full support for integration and domain events
- **CloudEvents Compliance**: Industry-standard event format with automatic serialization
- **Automatic Topic Management**: Environment-aware topic naming and auto-provisioning
- **Partition Key Support**: Intelligent message partitioning for scalability
- **OpenTelemetry Integration**: Built-in observability and distributed tracing
- **Health Checks**: Kafka connectivity monitoring
- **WolverineFx Integration**: CQRS/MediatR-style message handling

## Integrated Dependencies

This package includes the following key dependencies:

| Package | Purpose |
|---------|---------|
| **Aspire.Confluent.Kafka** | .NET Aspire Kafka integration with service discovery |
| **CloudNative.CloudEvents.Kafka** | CloudEvents specification implementation for Kafka |
| **WolverineFx.Kafka** | Message bus framework with Kafka transport |
| **Confluent.Kafka.Extensions.OpenTelemetry** | OpenTelemetry instrumentation for Kafka clients |

## Getting Started

### Prerequisites

- .NET 9.0 or later
- Apache Kafka 2.8 or later
- Momentum.ServiceDefaults package

### Basic Setup

Add Kafka messaging to your Momentum service:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add service defaults first
builder.AddServiceDefaults();

// Add Kafka messaging
builder.AddKafkaMessagingExtensions();

var app = builder.Build();

app.MapDefaultEndpoints();
app.Run();
```

### 2. Configuration

Add the Kafka connection string to your configuration:

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Messaging": "localhost:9092"
  }
}
```

### 3. Define Integration Events

Create events that will be published across services:

```csharp
// Events should be in a namespace ending with "IntegrationEvents"
namespace MyService.Contracts.IntegrationEvents;

[EventTopic("customer", Domain = "sales")]
public record CustomerCreated(
    Guid CustomerId,
    string Name,
    string Email,
    DateTime CreatedAt) : IDistributedEvent
{
    public string GetPartitionKey() => CustomerId.ToString();
}
```

### 4. Publishing Events

Publish events using Wolverine's message bus:

```csharp
public class CustomerService(IMessageBus messageBus)
{
    public async Task CreateCustomerAsync(CreateCustomerRequest request)
    {
        // Business logic here...
        
        var integrationEvent = new CustomerCreated(
            customerId,
            request.Name,
            request.Email,
            DateTime.UtcNow);

        // This will be automatically routed to the appropriate Kafka topic
        await messageBus.PublishAsync(integrationEvent);
    }
}
```

### 5. Handling Events

Create handlers for integration events:

```csharp
// This handler will automatically subscribe to the CustomerCreated topic
public class CustomerCreatedHandler
{
    public async Task Handle(CustomerCreated customerCreated, CancellationToken cancellationToken)
    {
        // Process the integration event
        Console.WriteLine($"Customer {customerCreated.Name} was created with ID {customerCreated.CustomerId}");
    }
}
```

## Advanced Configuration

### Topic Naming Convention

Topics are automatically named using the following pattern:
```
{environment}.{domain}.{scope}.{topic}.{version}
```

For example:
- Development: `dev.sales.public.customers.v1`
- Production: `prod.sales.public.customers.v1`
- Internal events: `prod.sales.internal.customer-updates.v1`

### Event Topic Attributes

Control topic configuration with attributes:

```csharp
[EventTopic(
    "order-payment", 
    Domain = "ecommerce",
    Internal = false,           // Creates public topic
    ShouldPluralizeTopicName = true,  // "payments" instead of "payment"
    Version = "v2")]
public record PaymentProcessed(Guid OrderId, decimal Amount);
```

### Partition Key Strategies

#### Using IDistributedEvent Interface

```csharp
public record OrderCreated(Guid OrderId, Guid CustomerId) : IDistributedEvent
{
    // Messages with the same customer ID will go to the same partition
    public string GetPartitionKey() => CustomerId.ToString();
}
```

#### Using PartitionKey Attribute

```csharp
public record ProductUpdated(
    [PartitionKey] Guid ProductId,
    string Name,
    decimal Price);
```

### Environment-Specific Configuration

The package automatically adapts topic names based on the environment:

```csharp
// Environment mapping
"Development" → "dev"
"Production" → "prod"
"Test" → "test"
```

### Health Checks

Kafka health checks are automatically registered and available at the `/health` endpoint:

```json
{
  "status": "Healthy",
  "checks": {
    "kafka": {
      "status": "Healthy",
      "description": "Kafka connectivity check",
      "tags": ["messaging", "kafka"]
    }
  }
}
```

## CloudEvents Integration

All messages are automatically wrapped in CloudEvents format, providing:

- **Standardization**: Industry-standard event format
- **Metadata**: Rich event metadata (source, type, time, etc.)
- **Tracing**: Distributed tracing correlation
- **Versioning**: Event schema evolution support

Example CloudEvent structure:

```json
{
  "specversion": "1.0",
  "type": "CustomerCreated",
  "source": "urn:momentum:sales-api",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "time": "2024-01-15T10:30:00Z",
  "datacontenttype": "application/json",
  "data": {
    "customerId": "123e4567-e89b-12d3-a456-426614174000",
    "name": "John Doe",
    "email": "john.doe@example.com"
  }
}
```

## Error Handling

The package includes built-in error handling and resilience:

- **Automatic Retry**: Failed message processing with exponential backoff
- **Dead Letter Queues**: Poison message handling
- **Circuit Breaker**: Protection against downstream failures
- **Telemetry**: Comprehensive error tracking and metrics

## Observability

Built-in observability includes:

### Metrics
- Message publish/consume rates
- Processing latency
- Error rates
- Topic partition metrics

### Tracing
- End-to-end message flow tracking
- Cross-service correlation
- Performance bottleneck identification

### Logging
- Structured logging with correlation IDs
- Event processing lifecycle
- Error diagnostics

## Best Practices

### Event Design
- **Immutable Events**: Events represent facts that have already occurred
- **Rich Context**: Include all necessary information in the event
- **Backward Compatibility**: Design events for schema evolution

### Partition Keys
- **Consistency**: Use consistent partition keys for related events
- **Distribution**: Ensure good key distribution to avoid hot partitions
- **Stability**: Partition keys should be stable over time

### Topic Management
- **Environment Separation**: Always use environment-specific topics
- **Naming Conventions**: Follow the established topic naming pattern
- **Retention**: Configure appropriate message retention policies

## Troubleshooting

### Common Issues

**Connection Failures**
```
InvalidOperationException: Kafka connection string 'Messaging' not found in configuration
```
- Ensure the `Messaging` connection string is configured
- Verify Kafka broker accessibility

**Topic Creation Issues**
```
Topic does not exist and auto-creation is disabled
```
- Enable auto-provisioning in Kafka configuration
- Manually create topics if auto-creation is disabled

**Serialization Errors**
```
CloudEvent serialization failed
```
- Ensure event types are properly decorated with attributes
- Verify JSON serialization compatibility

### Debug Logging

Enable debug logging for detailed troubleshooting:

```json
{
  "Logging": {
    "LogLevel": {
      "Momentum.Extensions.Messaging.Kafka": "Debug",
      "Wolverine.Kafka": "Debug"
    }
  }
}
```

## Requirements

- **.NET 9.0** or later
- **Apache Kafka 2.8** or later
- **Momentum.ServiceDefaults** package

## Related Packages

- **Momentum.ServiceDefaults** - Base service configuration
- **Momentum.Extensions.Abstractions** - Shared abstractions and interfaces

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/vgmello/momentum-sample/blob/main/LICENSE) file for details.

## Contributing

For more information about the Momentum platform and contribution guidelines, please visit the [main repository](https://github.com/vgmello/momentum-sample).