## Basic E-commerce Order Processing Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure core messaging infrastructure
builder.AddWolverine(opts =>
{
    // Configure custom routing for order events
    opts.PublishMessage<OrderCreated>()
        .ToKafkaTopic("ecommerce.orders.order-created")
        .UseDurableOutbox();
        
    // Configure local queues for background processing
    opts.LocalQueue("order-processing")
        .UseDurableInbox()
        .ProcessInline();
});

var app = builder.Build();
await app.RunAsync(args);
```

## Command Handler with Automatic Validation

```csharp
// Command definition with validation
public record CreateOrder(Guid CustomerId, List<OrderItem> Items);

public class CreateOrderValidator : AbstractValidator<CreateOrder>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().Must(items => items.Count <= 50);
    }
}

// Handler - validation happens automatically before execution
public static class OrderHandlers
{
    public static async Task<OrderCreated> Handle(
        CreateOrder command,
        IOrderRepository orders,
        IMessageBus bus,
        ILogger<OrderHandlers> logger)
    {
        // Command is guaranteed to be valid
        var order = new Order(command.CustomerId, command.Items);
        await orders.SaveAsync(order);
        
        // Publish integration event for other services
        await bus.PublishAsync(new OrderCreated(order.Id, order.CustomerId));
        
        logger.LogInformation("Order {OrderId} created for customer {CustomerId}", 
            order.Id, order.CustomerId);
            
        return new OrderCreated(order.Id, order.CustomerId);
    }
}
```

## Event Handler for Cross-Service Integration

```csharp
// Integration event from external service
[EventTopic("ecommerce.payments.payment-completed")]
public record PaymentCompleted(Guid OrderId, decimal Amount);

// Handler automatically discovered and registered
public static class PaymentHandlers
{
    [KafkaListener("ecommerce.payments")]
    public static async Task Handle(
        PaymentCompleted @event,
        IOrderRepository orders,
        IMessageBus bus)
    {
        var order = await orders.GetByIdAsync(@event.OrderId);
        order.MarkAsPaid(@event.Amount);
        await orders.SaveAsync(order);
        
        // Trigger fulfillment process
        await bus.SendToQueueAsync("fulfillment", 
            new FulfillOrder(order.Id, order.Items));
    }
}
```

## Configuration for Multi-Service Architecture

```json
// appsettings.json
{
  "ConnectionStrings": {
    "ServiceBus": "Host=postgres;Database=order_service_messaging;Username=app;Password=secret"
  },
  "ServiceBus": {
    "Domain": "ECommerce",
    "PublicServiceName": "order-service",
    "CloudEvents": {
      "Source": "https://api.mystore.com/orders",
      "DefaultType": "com.mystore.orders"
    }
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "GroupId": "order-service-v1"
  }
}
```

## Manual Service Registration for Testing

```csharp
// In test setup
public class OrderServiceIntegrationTests : IClassFixture<TestFixture>
{
    private readonly IServiceProvider _services;
    
    public OrderServiceIntegrationTests(TestFixture fixture)
    {
        var services = new ServiceCollection();
        var config = fixture.Configuration;
        var env = fixture.Environment;
        
        // Add Wolverine with test-specific configuration
        services.AddWolverineWithDefaults(env, config, opts =>
        {
            // Use in-memory transport for testing
            opts.UseInMemoryTransport();
            
            // Disable external dependencies
            opts.DisableKafka();
            
            // Enable immediate processing for synchronous testing
            opts.Policies.DisableConventionalLocalRouting();
        });
        
        _services = services.BuildServiceProvider();
    }
}
```

## Environment-Specific Configuration

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "ServiceBus": "Host=localhost;Database=dev_messaging;Username=dev;Password=dev"
  },
  "ServiceBus": {
    "PublicServiceName": "order-service-dev"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  }
}

// appsettings.Production.json  
{
  "ConnectionStrings": {
    "ServiceBus": "${MESSAGING_CONNECTION_STRING}"
  },
  "ServiceBus": {
    "PublicServiceName": "order-service",
    "CloudEvents": {
      "Source": "https://api.production.mystore.com/orders"
    }
  },
  "Kafka": {
    "BootstrapServers": "${KAFKA_BOOTSTRAP_SERVERS}",
    "SecurityProtocol": "SaslSsl",
    "SaslMechanism": "Plain"
  }
}
```