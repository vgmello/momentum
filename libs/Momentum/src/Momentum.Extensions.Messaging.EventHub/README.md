# Momentum.Extensions.Messaging.EventHub

Azure Event Hubs messaging integration for Momentum platform providing event-driven communication through Azure Event Hubs with CloudEvents and WolverineFx support.

## Features

- **Full Wolverine Transport**: Complete transport implementation for Azure Event Hubs
- **CloudEvents Support**: Standard event format for interoperability
- **Aspire Integration**: Seamless connection string management and resource orchestration
- **Convention-based Routing**: Automatic event discovery via `EventTopic` and `PartitionKey` attributes
- **Azure Blob Storage Checkpointing**: Reliable message processing with at-least-once delivery
- **Auto-provisioning**: Development-friendly automatic Event Hub creation (non-production only)
- **Partition Key Support**: Intelligent message routing to partitions

## Installation

```bash
dotnet add package Momentum.Extensions.Messaging.EventHub
```

## Quick Start

### 1. Configure Event Hubs in appsettings.json

```json
{
  "ConnectionStrings": {
    "Messaging": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=...",
    "Messaging-checkpoints": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=..."
  },
  "ServiceBus": {
    "Domain": "YourDomain",
    "PublicServiceName": "your-service",
    "Wolverine": {
      "AutoProvision": false
    }
  }
}
```

### 2. Add Event Hub Messaging Extensions

```csharp
using Momentum.Extensions.Messaging.EventHub.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add Momentum service defaults with Event Hubs
builder.AddServiceDefaults();
builder.AddServiceBus();
builder.AddEventHubMessagingExtensions();

var app = builder.Build();
app.Run();
```

### 3. Define Integration Events

```csharp
using Momentum.Extensions.Abstractions.Messaging;

namespace YourService.Contracts.IntegrationEvents;

[EventTopic("customer-created", Internal = false)]
public record CustomerCreated(
    [PartitionKey] Guid CustomerId,
    string Name,
    string Email
);
```

### 4. Publish Events

```csharp
using Wolverine;

public class CustomerService
{
    private readonly IMessageBus _messageBus;

    public CustomerService(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public async Task CreateCustomerAsync(CreateCustomerCommand command)
    {
        // Create customer...

        // Publish integration event
        await _messageBus.PublishAsync(new CustomerCreated(
            customer.Id,
            customer.Name,
            customer.Email
        ));
    }
}
```

### 5. Handle Events

```csharp
namespace YourService.Customers;

public class CustomerCreatedHandler
{
    private readonly ILogger<CustomerCreatedHandler> _logger;

    public CustomerCreatedHandler(ILogger<CustomerCreatedHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(CustomerCreated @event)
    {
        _logger.LogInformation(
            "Customer created: {CustomerId} - {Name}",
            @event.CustomerId,
            @event.Name
        );

        // Handle the event...
    }
}
```

## Configuration

### Aspire Configuration

Event Hubs can be configured using .NET Aspire:

```json
{
  "Aspire": {
    "Azure": {
      "EventHubs": {
        "Messaging": {
          "FullyQualifiedNamespace": "your-namespace.servicebus.windows.net",
          "UseDefaultCredential": true,
          "Producer": {
            "Messaging": {
              "Options": {
                "RetryOptions": {
                  "MaximumRetries": 3
                }
              }
            }
          },
          "Processor": {
            "Messaging": {
              "ConsumerGroup": "$Default",
              "Options": {
                "MaximumWaitTime": "00:00:01"
              }
            }
          }
        }
      },
      "BlobStorage": {
        "Messaging-checkpoints": {
          "ServiceUri": "https://youraccount.blob.core.windows.net",
          "UseDefaultCredential": true
        }
      }
    }
  }
}
```

### Manual Configuration

```csharp
using Azure.Identity;
using Momentum.Extensions.Messaging.EventHub.Transport;

builder.Services.AddWolverine(opts =>
{
    opts.UseEventHub("your-namespace.servicebus.windows.net", new DefaultAzureCredential())
        .AutoProvision(); // Development only!

    opts.ConfigureEventHub(transport =>
    {
        transport.CheckpointBlobContainerUri = "https://youraccount.blob.core.windows.net/checkpoints";
        transport.CheckpointCredential = new DefaultAzureCredential();
    });
});
```

### Listener Configuration

```csharp
builder.Services.AddWolverine(opts =>
{
    opts.ListenToEventHub("dev.ecommerce.public.orders.v1")
        .ConsumerGroup("order-processor")
        .BufferedInMemory()
        .Partitions(4)
        .Retention(TimeSpan.FromDays(7));
});
```

### Publisher Configuration

```csharp
builder.Services.AddWolverine(opts =>
{
    opts.PublishMessage<OrderCreated>()
        .ToEventHub("dev.ecommerce.public.orders.v1");
});
```

## Event Hub Naming Convention

Event Hub names follow the Momentum convention:

**Format**: `{env}.{domain}.{scope}.{topic}.{version}`

**Example**: `dev.e-commerce.public.orders.v1`

Components:
- **env**: Environment (dev, staging, prod)
- **domain**: Business domain from `ServiceBusOptions.Domain` or `[DefaultDomain]` attribute
- **scope**: "internal" or "public" from `EventTopicAttribute`
- **topic**: From `EventTopicAttribute.Topic` (optionally pluralized)
- **version**: Optional version suffix

## CloudEvents Support

All messages are automatically wrapped in CloudEvents format for interoperability:

```json
{
  "specversion": "1.0",
  "type": "YourService.Contracts.IntegrationEvents.CustomerCreated",
  "source": "/your-domain/your-service",
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "time": "2025-10-25T13:30:00Z",
  "datacontenttype": "application/json",
  "data": {
    "customerId": "789e4567-e89b-12d3-a456-426614174999",
    "name": "John Doe",
    "email": "john@example.com"
  }
}
```

## Partition Keys

Partition keys ensure related messages are processed in order:

```csharp
[EventTopic("order-events")]
public record OrderCreated(
    [PartitionKey(Order = 1)] Guid TenantId,  // Primary partition key
    [PartitionKey(Order = 2)] Guid OrderId,    // Secondary partition key
    decimal Amount
);

// Composite partition key: "tenant-id|order-id"
```

Or implement `IDistributedEvent`:

```csharp
public record OrderCreated(Guid TenantId, Guid OrderId, decimal Amount) : IDistributedEvent
{
    public string GetPartitionKey() => TenantId.ToString();
}
```

## Auto-Provisioning

Auto-provisioning creates Event Hubs automatically in development:

```json
{
  "ServiceBus": {
    "Wolverine": {
      "AutoProvision": true  // DEVELOPMENT ONLY
    }
  }
}
```

⚠️ **Warning**: Auto-provisioning is blocked in production environments for safety.

## Migration from Kafka

If you're migrating from `Momentum.Extensions.Messaging.Kafka`:

| Kafka | Event Hubs | Notes |
|-------|------------|-------|
| `AddKafkaMessagingExtensions()` | `AddEventHubMessagingExtensions()` | Same pattern |
| `UseKafka(connectionString)` | `UseEventHub(connectionString)` | Connection string format differs |
| `ListenToKafkaTopic(name)` | `ListenToEventHub(name)` | Event Hubs use "Event Hub" instead of "topic" |
| `ToKafkaTopic(name)` | `ToEventHub(name)` | - |
| PostgreSQL for Wolverine persistence | Blob Storage for checkpoints | Required for consumers |

Key differences:
- **Checkpointing**: Event Hubs uses Azure Blob Storage instead of PostgreSQL
- **Partitions**: Event Hubs have fixed partition counts (set at creation)
- **Topic vs Event Hub**: Terminology difference, but same concept

## Best Practices

1. **Use DefaultAzureCredential**: Prefer managed identities over connection strings in production
2. **Checkpoint Frequency**: Default checkpointing after each message ensures reliability
3. **Partition Count**: Choose partition count based on expected throughput (4-32 partitions typically)
4. **Consumer Groups**: Use separate consumer groups for different processing concerns
5. **Message Retention**: Default 7 days is usually sufficient; adjust based on replay needs
6. **Error Handling**: Failed messages are not checkpointed and will be reprocessed (at-least-once delivery)

## Troubleshooting

### Consumer Not Receiving Messages

- Verify Blob Storage checkpoint configuration
- Check consumer group name (must match configuration)
- Ensure Event Hub exists and has messages
- Check Azure credentials have appropriate permissions

### Messages Not Being Sent

- Verify Event Hub connection string/credentials
- Check Event Hub exists
- Ensure partition key is valid
- Check for exceptions in logs

### Checkpoint Errors

- Verify Blob Storage connection string/credentials
- Ensure container exists or auto-provision is enabled
- Check Azure credentials have Blob Storage permissions

## Architecture

The Event Hub transport follows Wolverine's `BrokerTransport` pattern:

- **EventHubTransport**: Main transport orchestrator
- **EventHubEndpoint**: Individual Event Hub endpoint configuration
- **InlineEventHubSender**: Synchronous message sending
- **EventHubSenderProtocol**: Batched message sending for throughput
- **EventHubListener**: Event processor with checkpointing
- **CloudEventMapper**: CloudEvents ↔ EventData ↔ Wolverine Envelope mapping

## Requirements

- .NET 9.0
- Azure Event Hubs namespace
- Azure Blob Storage account (for consumers)
- Momentum.ServiceDefaults
- WolverineFx

## License

Copyright (c) Momentum .NET. All rights reserved.

## Related Packages

- [Momentum.Extensions.Messaging.Kafka](../Momentum.Extensions.Messaging.Kafka) - Kafka transport
- [Momentum.ServiceDefaults](../Momentum.ServiceDefaults) - Core service infrastructure
- [Momentum.Extensions.Abstractions](../Momentum.Extensions.Abstractions) - Shared abstractions

## Support

For issues and questions, please visit the [Momentum repository](https://github.com/vgmello/momentum).
