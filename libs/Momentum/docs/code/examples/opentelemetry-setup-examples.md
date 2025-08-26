## Basic Usage with Default Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Adds complete observability stack
builder.AddOpenTelemetry();

var app = builder.Build();

// Your traces will now include:
// - HTTP request spans with timing
// - Database query spans (if using Entity Framework)
// - Custom business logic spans
app.MapGet("/orders/{id}", async (int id, ActivitySource activitySource) =>
{
    using var activity = activitySource.StartActivity("GetOrder");
    activity?.SetTag("order.id", id);
    
    // This operation will be traced
    return await GetOrderById(id);
});

await app.RunAsync(args);
```

## Custom Configuration for Production

```json
// appsettings.Production.json
{
  "OpenTelemetry": {
    "ActivitySourceName": "ECommerce.OrderService",
    "MessagingMeterName": "ECommerce.Orders.Messaging"
  },
  "OTEL_EXPORTER_OTLP_ENDPOINT": "http://otel-collector:4317",
  "OTEL_SERVICE_NAME": "order-service",
  "OTEL_RESOURCE_ATTRIBUTES": "deployment.environment=production,service.version=1.2.0"
}
```

## Custom Metrics in Business Logic

```csharp
public class OrderService(Meter orderMeter)
{
    private readonly Counter<int> _ordersProcessed = 
        orderMeter.CreateCounter<int>("orders_processed_total");
    private readonly Histogram<double> _orderValue = 
        orderMeter.CreateHistogram<double>("order_value_dollars");
        
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order(request);
        await _repository.SaveAsync(order);
        
        // Custom metrics for business insights
        _ordersProcessed.Add(1, new("customer_type", request.CustomerType));
        _orderValue.Record(order.TotalValue, new("product_category", order.PrimaryCategory));
        
        return order;
    }
}
```

## Distributed Tracing Across Services

```csharp
// Service A (Order API)
app.MapPost("/orders", async (CreateOrderRequest request, HttpClient httpClient) =>
{
    // Create order locally
    var order = await CreateOrder(request);
    
    // Call inventory service - trace context automatically propagated
    await httpClient.PostAsJsonAsync("http://inventory-service/reserve", 
        new { OrderId = order.Id, Items = order.Items });
        
    return order;
});

// Service B (Inventory API) - receives trace context automatically
app.MapPost("/reserve", async (ReserveItemsRequest request) =>
{
    // This operation appears as child span in the same trace
    return await ReserveInventory(request);
});
```

## Troubleshooting Common Issues

```csharp
// Problem: No traces appearing in collector
// Solution: Check OTLP endpoint and network connectivity

// Problem: Too many traces in production
// Solution: Sampling is automatically set to 10% in non-development environments

// Problem: Missing custom spans
// Solution: Ensure ActivitySource is injected and spans are disposed
using var activity = activitySource.StartActivity("MyOperation");
// ... work here ...
// Dispose happens automatically with 'using'

// Problem: Missing HTTP client traces
// Solution: Paths containing these patterns are excluded by default:
// - /OrleansSiloInstances (Orleans infrastructure)
// - /$batch (OData batch operations)
```