# Integration Events in Momentum

Integration events enable communication between different services or bounded contexts in your system. They represent significant business events that other services might need to react to.

## What are Integration Events?

Integration events are:
- **Cross-service communication**: Messages sent between different services
- **Business significant**: Represent important business events like "CashierCreated" or "InvoicePaid"
- **Asynchronous**: Processed in the background without blocking the caller
- **Decoupling mechanism**: Allow services to communicate without direct dependencies

## Event Definition

Integration events are defined as records with specific attributes:

```csharp
[EventTopic<Cashier>]
public record CashierCreated(
    [PartitionKey] Guid TenantId,
    Cashier Cashier
);
```

### Key Components

1. **EventTopic Attribute**: Defines the topic and routing configuration
2. **PartitionKey Attribute**: Ensures message ordering and proper distribution
3. **XML Documentation**: Required for auto-generated documentation
4. **Immutable Record**: Events should never change once created

## Event Attributes

### EventTopic Attribute

The `EventTopic` attribute configures how the event is routed and published:

```csharp
// Basic usage
[EventTopic<Cashier>]
public record CashierCreated(/* parameters */);

// With custom configuration
[EventTopic<Invoice>(
    Topic = "invoice-updates",
    Domain = "invoicing", 
    Version = "v2",
    Internal = false,
    ShouldPluralizeTopicName = true
)]
public record InvoiceUpdated(/* parameters */);
```

**Parameters:**
- `Topic`: Custom topic name (defaults to class name)
- `Domain`: Business domain (defaults to assembly domain)
- `Version`: Event schema version
- `Internal`: Whether the event is internal to the service
- `ShouldPluralizeTopicName`: Whether to pluralize the topic name

### PartitionKey Attribute

The `PartitionKey` attribute ensures proper message ordering and distribution:

```csharp
public record CashierCreated(
    [PartitionKey] Guid TenantId,  // Single partition key
    Cashier Cashier
);

public record ComplexEvent(
    [PartitionKey(Order = 0)] Guid TenantId,      // Primary partition key
    [PartitionKey(Order = 1)] int RegionId,       // Secondary partition key
    string EventData
);
```

**Benefits of Partition Keys:**
- **Message ordering**: Messages with the same partition key are processed in order
- **Load balancing**: Events are distributed across Kafka partitions
- **Tenant isolation**: Multi-tenant applications can isolate by tenant

## Real-World Examples

### Simple Event

```csharp
[EventTopic<Guid>]
public record CashierDeleted(
    [PartitionKey] Guid TenantId, 
    Guid CashierId
);
```

### Complex Event with Multiple Partition Keys

```csharp
[EventTopic<Cashier>]
public record CashierCreated(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] int PartitionKeyTest,
    Cashier Cashier
);
```

### Invoice Events

```csharp
[EventTopic<Invoice>]
public record InvoiceCreated(
    [PartitionKey] Guid TenantId,
    Invoice Invoice
);

[EventTopic<Guid>]
public record InvoicePaid(
    [PartitionKey] Guid TenantId,
    Guid InvoiceId,
    decimal AmountPaid,
    DateTime PaidDate
);
```

## Publishing Events

Integration events are automatically published when returned from command handlers:

```csharp
public static class CreateCashierCommandHandler
{
    public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
        CreateCashierCommand command, 
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // Execute business logic
        var dbCommand = CreateInsertCommand(command);
        var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedCashier.ToModel();
        
        // Create integration event
        var createdEvent = new CashierCreated(
            result.TenantId, 
            PartitionKeyTest: 0, 
            result
        );

        // Return result and event - framework will publish the event automatically
        return (result, createdEvent);
    }
}
```

### Manual Event Publishing

You can also publish events manually using the message bus:

```csharp
public static class SomeService
{
    public static async Task DoSomethingAsync(
        IMessageBus messageBus, 
        CancellationToken cancellationToken)
    {
        // Your business logic here
        
        // Publish event manually
        var event = new CashierCreated(tenantId, 0, cashier);
        await messageBus.PublishAsync(event, cancellationToken);
    }
}
```

## Event Handlers

Other services can subscribe to integration events by creating handlers:

```csharp
// In another service (e.g., Notification Service)
public static class CashierCreatedHandler
{
    public static async Task Handle(
        CashierCreated cashierCreated, 
        IEmailService emailService,
        ILogger<CashierCreatedHandler> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing CashierCreated event for tenant {TenantId}", 
            cashierCreated.TenantId);

        try
        {
            await emailService.SendWelcomeEmailAsync(
                cashierCreated.Cashier.Email,
                cashierCreated.Cashier.Name,
                cancellationToken);

            logger.LogInformation("Welcome email sent to {Email}", 
                cashierCreated.Cashier.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", 
                cashierCreated.Cashier.Email);
            throw; // Re-throw to trigger retry logic
        }
    }
}
```

## Topic Naming Convention

Momentum automatically generates Kafka topic names based on the event configuration:

**Format:** `{environment}.{domain}.{scope}.{topic}.{version}`

**Examples:**
- `dev.appdomain.public.cashiers.v1`
- `prod.invoicing.internal.payments`
- `test.notifications.public.emails.v2`

### Topic Name Components

- **Environment**: `dev`, `test`, `prod` (based on hosting environment)
- **Domain**: Business domain (from EventTopic attribute or assembly default)
- **Scope**: `public` (cross-service) or `internal` (service-specific)
- **Topic**: Event name (pluralized by default)
- **Version**: Schema version (optional)

## Event Versioning

Handle event schema evolution with versioning:

```csharp
// Version 1
[EventTopic<User>(Version = "v1")]
public record UserCreated(
    [PartitionKey] Guid TenantId,
    Guid UserId,
    string Name,
    string Email
);

// Version 2 - added new field
[EventTopic<User>(Version = "v2")]
public record UserCreated(
    [PartitionKey] Guid TenantId,
    Guid UserId,
    string Name,
    string Email,
    DateTime CreatedDate  // New field
);
```

### Handling Multiple Versions

```csharp
// Handler for V1 events
public static class UserCreatedV1Handler
{
    public static async Task Handle(UserCreatedV1 userCreated, CancellationToken cancellationToken)
    {
        // Handle V1 event
    }
}

// Handler for V2 events
public static class UserCreatedV2Handler
{
    public static async Task Handle(UserCreatedV2 userCreated, CancellationToken cancellationToken)
    {
        // Handle V2 event
    }
}
```

## Error Handling and Retry

Integration event handlers support automatic retry and error handling:

```csharp
public static class OrderCreatedHandler
{
    public static async Task Handle(
        OrderCreated orderCreated,
        IInventoryService inventoryService,
        ILogger<OrderCreatedHandler> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await inventoryService.ReserveItemsAsync(
                orderCreated.OrderItems, 
                cancellationToken);
        }
        catch (InventoryNotAvailableException ex)
        {
            logger.LogWarning("Inventory not available for order {OrderId}: {Message}", 
                orderCreated.OrderId, ex.Message);
            
            // Don't retry for business exceptions
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process order created event for order {OrderId}", 
                orderCreated.OrderId);
            
            // Re-throw to trigger retry
            throw;
        }
    }
}
```

## Testing Integration Events

### Testing Event Publishing

```csharp
[Test]
public async Task Handle_ValidCommand_PublishesIntegrationEvent()
{
    // Arrange
    var command = new CreateCashierCommand(
        Guid.NewGuid(), 
        "John Doe", 
        "john@example.com"
    );

    var mockMessaging = new Mock<IMessageBus>();
    // ... setup mocks

    // Act
    var (result, integrationEvent) = await CreateCashierCommandHandler.Handle(
        command, mockMessaging.Object, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    integrationEvent.Should().NotBeNull();
    integrationEvent!.TenantId.Should().Be(command.TenantId);
    integrationEvent.Cashier.Name.Should().Be(command.Name);
}
```

### Testing Event Handlers

```csharp
[Test]
public async Task Handle_CashierCreated_SendsWelcomeEmail()
{
    // Arrange
    var cashierCreated = new CashierCreated(
        Guid.NewGuid(),
        0,
        new Cashier 
        { 
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john@example.com"
        }
    );

    var mockEmailService = new Mock<IEmailService>();
    var logger = new Mock<ILogger<CashierCreatedHandler>>();

    // Act
    await CashierCreatedHandler.Handle(
        cashierCreated, 
        mockEmailService.Object, 
        logger.Object, 
        CancellationToken.None);

    // Assert
    mockEmailService.Verify(
        x => x.SendWelcomeEmailAsync(
            cashierCreated.Cashier.Email,
            cashierCreated.Cashier.Name,
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

## Best Practices

### Event Design

1. **Make events immutable**: Use records with readonly properties
2. **Include necessary data**: Events should contain all data consumers need
3. **Use meaningful names**: Event names should clearly describe what happened
4. **Version your events**: Plan for schema evolution from the beginning

### Partition Keys

1. **Choose wisely**: Partition keys affect message ordering and distribution
2. **Tenant isolation**: Use tenant ID as partition key for multi-tenant systems
3. **Avoid hotspots**: Don't use partition keys that create uneven distribution
4. **Keep it stable**: Partition keys should not change for the same logical entity

### Error Handling

1. **Handle business exceptions**: Don't retry for expected business failures
2. **Log appropriately**: Log enough information for debugging
3. **Use dead letter queues**: Configure DLQ for failed messages
4. **Implement circuit breakers**: Protect downstream services

### Performance

1. **Keep events small**: Large events can impact performance
2. **Batch when possible**: Consider batching related events
3. **Monitor throughput**: Watch for processing bottlenecks
4. **Use appropriate timeouts**: Set reasonable timeouts for external calls

### Documentation

1. **Use XML documentation**: Document when events are published
2. **Include examples**: Show example event payloads
3. **Document handlers**: Explain what each handler does
4. **Keep it current**: Update documentation when events change

## Configuration

Integration events are configured automatically through:

1. **Service discovery**: Events are discovered from domain assemblies
2. **Topic configuration**: Kafka topics are auto-provisioned
3. **Consumer groups**: Each service gets its own consumer group
4. **Serialization**: CloudEvents format with System.Text.Json

See [Kafka Configuration](./kafka) for detailed Kafka setup instructions.

## Next Steps

- Learn about [Domain Events](./domain-events) for internal service events
- Understand [Kafka Configuration](./kafka) for message broker setup
- Explore [Wolverine](./wolverine) messaging framework details
- See [Testing](../testing/) for comprehensive testing strategies