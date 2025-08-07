# Kafka Configuration in Momentum

Apache Kafka serves as the message broker for integration events in Momentum, enabling reliable, scalable communication between services.

## Overview

Momentum integrates with Kafka through:
- **Automatic configuration**: Kafka is configured automatically when connection strings are provided
- **Topic management**: Topics are auto-provisioned based on event attributes
- **CloudEvents format**: Messages use CloudEvents specification for interoperability
- **Consumer groups**: Each service gets its own consumer group for scalable processing
- **Health checks**: Built-in health monitoring for Kafka connectivity

## Configuration

### Connection String

Configure Kafka in your application settings:

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Messaging": "localhost:9092"
  }
}
```

### Environment-Specific Configuration

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "Messaging": "localhost:9092"
  }
}

// appsettings.Production.json
{
  "ConnectionStrings": {
    "Messaging": "prod-kafka-cluster:9092"
  }
}
```

### Advanced Kafka Settings

For more complex scenarios, you can configure additional Kafka options:

```json
{
  "ConnectionStrings": {
    "Messaging": "broker1:9092,broker2:9092,broker3:9092"
  },
  "Kafka": {
    "SecurityProtocol": "SaslSsl",
    "SaslMechanism": "Plain",
    "SaslUsername": "your-username",
    "SaslPassword": "your-password",
    "SslCaLocation": "/path/to/ca-cert.pem"
  }
}
```

## Topic Management

### Automatic Topic Creation

Momentum automatically creates Kafka topics based on your integration events:

```csharp
[EventTopic<Cashier>]
public record CashierCreated(
    [PartitionKey] Guid TenantId,
    Cashier Cashier
);
```

This creates a topic named: `dev.appdomain.public.cashiers`

### Topic Naming Convention

Topics follow this pattern: `{environment}.{domain}.{scope}.{topic}.{version}`

**Components:**
- **Environment**: `dev`, `test`, `prod` (from hosting environment)
- **Domain**: Business domain (from EventTopic attribute or assembly default)
- **Scope**: `public` (cross-service) or `internal` (service-specific)
- **Topic**: Event name (typically pluralized)
- **Version**: Schema version (optional)

### Customizing Topic Names

```csharp
// Custom topic configuration
[EventTopic<Invoice>(
    Topic = "invoice-events",           // Custom topic name
    Domain = "billing",                 // Custom domain
    Version = "v2",                     // Specific version
    Internal = false,                   // Public scope
    ShouldPluralizeTopicName = false    // Don't pluralize
)]
public record InvoiceUpdated(
    [PartitionKey] Guid TenantId,
    Invoice Invoice
);

// Results in topic: dev.billing.public.invoice-events.v2
```

### Topic Configuration Examples

```csharp
// Standard public event
[EventTopic<User>]
public record UserCreated([PartitionKey] Guid TenantId, User User);
// Topic: dev.appdomain.public.users

// Internal service event
[EventTopic<Order>(Internal = true)]
public record OrderValidated([PartitionKey] Guid TenantId, Guid OrderId);
// Topic: dev.appdomain.internal.orders

// Versioned event
[EventTopic<Payment>(Version = "v2")]
public record PaymentProcessed([PartitionKey] Guid TenantId, Payment Payment);
// Topic: dev.appdomain.public.payments.v2

// Custom domain event
[EventTopic<Notification>(Domain = "communications")]
public record EmailSent([PartitionKey] Guid TenantId, string Email);
// Topic: dev.communications.public.notifications
```

## Partition Strategy

### Partition Keys

Partition keys ensure message ordering and load distribution:

```csharp
// Single partition key
[EventTopic<Cashier>]
public record CashierCreated(
    [PartitionKey] Guid TenantId,  // Messages with same TenantId go to same partition
    Cashier Cashier
);

// Multiple partition keys (combined)
[EventTopic<Order>]
public record OrderCreated(
    [PartitionKey(Order = 0)] Guid TenantId,     // Primary key
    [PartitionKey(Order = 1)] Guid CustomerId,   // Secondary key
    Order Order
);
```

### Partition Key Benefits

1. **Ordering Guarantees**: Messages with the same partition key are processed in order
2. **Load Balancing**: Different partition keys distribute load across partitions
3. **Tenant Isolation**: Each tenant's messages can be processed independently
4. **Scaling**: More partitions allow more parallel consumers

### Choosing Partition Keys

**Good partition key choices:**
- Tenant ID (for multi-tenant applications)
- User ID (for user-specific events)
- Order ID (for order processing)
- Region ID (for geographic distribution)

**Avoid:**
- Timestamp-based keys (creates hot partitions)
- Sequential numbers (uneven distribution)
- Null or empty values

## Consumer Configuration

### Consumer Groups

Each service automatically gets its own consumer group:

```csharp
// Service name becomes consumer group ID
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(); // Configures consumer group as service name

var app = builder.Build();
// Consumer group: "MyInvoiceService" (based on application name)
```

### Consumer Settings

Momentum configures consumers with sensible defaults:

```csharp
// Default consumer configuration
consumer.GroupId = options.ServiceName;           // Service name as group ID
consumer.AutoOffsetReset = AutoOffsetReset.Latest; // Start from latest messages
consumer.EnableAutoCommit = true;                  // Automatically commit offsets
consumer.EnableAutoOffsetStore = false;           // Manual offset storage
```

### Custom Consumer Configuration

For advanced scenarios, you can customize consumer settings:

```csharp
// In Program.cs or startup configuration
builder.AddWolverine(opts =>
{
    opts.UseKafka("localhost:9092")
        .ConfigureConsumers(consumer =>
        {
            consumer.GroupId = "custom-group-name";
            consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
            consumer.EnableAutoCommit = false; // Manual commit
            consumer.SessionTimeoutMs = 30000;
            consumer.HeartbeatIntervalMs = 3000;
        });
});
```

## Message Format

### CloudEvents

Momentum uses CloudEvents format for message interoperability:

```json
{
  "specversion": "1.0",
  "type": "CashierCreated",
  "source": "appdomain-api",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "time": "2024-01-15T10:30:00Z",
  "datacontenttype": "application/json",
  "subject": "tenant/123e4567-e89b-12d3-a456-426614174000",
  "data": {
    "tenantId": "123e4567-e89b-12d3-a456-426614174000",
    "partitionKeyTest": 0,
    "cashier": {
      "id": "456e7890-e12b-34c5-d678-901234567890",
      "name": "John Doe",
      "email": "john.doe@example.com",
      "createdDate": "2024-01-15T10:30:00Z"
    }
  }
}
```

### Message Headers

Momentum adds standard headers to messages:

```csharp
// Headers automatically added
"ce-specversion": "1.0"
"ce-type": "CashierCreated"
"ce-source": "appdomain-api"
"ce-id": "unique-message-id"
"ce-time": "2024-01-15T10:30:00Z"
"content-type": "application/cloudevents+json"
```

## Error Handling

### Retry Configuration

Configure retry policies for Kafka consumers:

```csharp
builder.AddWolverine(opts =>
{
    opts.Policies.OnException<KafkaException>()
        .Retry(3)
        .Then.Requeue();
        
    opts.Policies.OnException<BusinessException>()
        .MoveToErrorQueue(); // Don't retry business exceptions
});
```

### Dead Letter Topics

Failed messages are moved to dead letter topics:

```csharp
// Dead letter topic naming: {original-topic}.dead-letter
// Example: dev.appdomain.public.cashiers.dead-letter
```

### Circuit Breaker

Protect against cascading failures:

```csharp
builder.Services.AddHttpClient<IExternalService>()
    .AddStandardResilienceHandler(); // Includes circuit breaker
```

## Monitoring and Health Checks

### Health Checks

Kafka health checks are automatically configured:

```csharp
// Health check endpoint: /health
// Checks Kafka connectivity and topic accessibility
```

### Metrics

Monitor Kafka performance with built-in metrics:

```csharp
// Available metrics:
// - kafka_consumer_lag
// - kafka_messages_consumed_total  
// - kafka_messages_produced_total
// - kafka_consumer_group_members
```

### Logging

Configure logging for Kafka operations:

```csharp
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Wolverine.Kafka": "Information",
      "Confluent.Kafka": "Warning"
    }
  }
}
```

## Development and Testing

### Local Development

For local development, use Docker Compose:

```yaml
# docker-compose.yml
version: '3.8'
services:
  zookeeper:
    image: confluentinc/cp-zookeeper:latest
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000

  kafka:
    image: confluentinc/cp-kafka:latest
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
```

Start with:
```bash
docker compose up -d kafka zookeeper
```

### Testing with Testcontainers

Use Testcontainers for integration tests:

```csharp
[Test]
public async Task Should_Process_Integration_Event()
{
    // Testcontainers automatically provides Kafka for testing
    using var testContext = new IntegrationTestContext();
    
    var messageBus = testContext.GetService<IMessageBus>();
    
    // Publish event
    var cashierCreated = new CashierCreated(Guid.NewGuid(), new Cashier());
    await messageBus.PublishAsync(cashierCreated);
    
    // Verify processing
    await testContext.WaitForMessageProcessing();
    
    // Assert handler was called
    var mockHandler = testContext.GetMock<ICashierHandler>();
    mockHandler.Verify(x => x.Handle(It.IsAny<CashierCreated>(), It.IsAny<CancellationToken>()), Times.Once);
}
```

## Production Deployment

### Cluster Configuration

For production, use a Kafka cluster:

```json
{
  "ConnectionStrings": {
    "Messaging": "kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092"
  }
}
```

### Security Configuration

Configure security for production:

```json
{
  "Kafka": {
    "SecurityProtocol": "SaslSsl",
    "SaslMechanism": "Plain",
    "SaslUsername": "${KAFKA_USERNAME}",
    "SaslPassword": "${KAFKA_PASSWORD}",
    "SslEndpointIdentificationAlgorithm": "https"
  }
}
```

### Performance Tuning

Optimize for production workloads:

```csharp
builder.AddWolverine(opts =>
{
    opts.UseKafka(connectionString)
        .ConfigureProducers(producer =>
        {
            producer.BatchSize = 16384;           // Batch size for throughput
            producer.LingerMs = 5;                // Wait time for batching
            producer.CompressionType = CompressionType.Snappy; // Compression
            producer.Acks = Acks.All;             // Durability
        })
        .ConfigureConsumers(consumer =>
        {
            consumer.FetchMinBytes = 1024;        // Minimum fetch size
            consumer.FetchMaxWaitMs = 500;        // Maximum wait time
            consumer.MaxPollIntervalMs = 300000;  // Max time between polls
        });
});
```

## Troubleshooting

### Common Issues

**Connection Problems:**
```bash
# Check Kafka connectivity
docker exec -it kafka-container kafka-topics --bootstrap-server localhost:9092 --list
```

**Topic Issues:**
```bash
# Create topic manually if needed
kafka-topics --bootstrap-server localhost:9092 --create --topic dev.appdomain.public.cashiers --partitions 3 --replication-factor 1
```

**Consumer Lag:**
```bash
# Check consumer group status
kafka-consumer-groups --bootstrap-server localhost:9092 --group your-service-name --describe
```

### Debugging

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Wolverine": "Debug",
      "Wolverine.Kafka": "Debug",
      "Confluent.Kafka": "Debug"
    }
  }
}
```

### Performance Issues

Monitor key metrics:
- Consumer lag
- Message throughput
- Error rates
- Partition distribution

## Best Practices

### Topic Design

1. **Plan partitions**: More partitions = more parallelism, but more overhead
2. **Choose good keys**: Partition keys should distribute load evenly
3. **Version topics**: Use versioning for schema evolution
4. **Monitor size**: Keep message sizes reasonable (< 1MB)

### Consumer Design

1. **Idempotent handlers**: Handlers should be safe to run multiple times
2. **Error handling**: Distinguish between retryable and non-retryable errors
3. **Performance**: Keep handlers fast or use background processing
4. **Monitoring**: Monitor consumer lag and processing times

### Security

1. **Use SSL/SASL**: Enable security for production
2. **Access control**: Use Kafka ACLs to control topic access
3. **Secrets management**: Store credentials securely
4. **Network security**: Secure network communication

### Operations

1. **Monitor health**: Use health checks and metrics
2. **Plan capacity**: Monitor disk, CPU, and network usage
3. **Backup strategy**: Plan for disaster recovery
4. **Version management**: Plan for Kafka version upgrades

## Next Steps

- Learn about [Wolverine](./wolverine) messaging framework integration
- Understand [Integration Events](./integration-events) publishing patterns
- Explore [Domain Events](./domain-events) for internal messaging
- See [Service Configuration](../service-configuration/) for advanced setup