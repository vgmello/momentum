## Basic E-commerce API Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure core service infrastructure
builder.AddServiceDefaults();

// Add your business services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddDbContext<EcommerceDbContext>();

var app = builder.Build();

// Map health check endpoints
app.MapDefaultHealthCheckEndpoints();

// Map your API endpoints
app.MapGroup("/api/orders").MapOrdersApi();

// Run with proper initialization and error handling
await app.RunAsync(args);
```

## Configuration in appsettings.json

```json
{
  "ConnectionStrings": {
    "ServiceBus": "Host=localhost;Database=ecommerce_messaging;Username=app;Password=secret"
  },
  "OpenTelemetry": {
    "ActivitySourceName": "ECommerceAPI",
    "MessagingMeterName": "ECommerce.Messaging"
  },
  "ServiceBus": {
    "Domain": "ECommerce",
    "PublicServiceName": "orders-api"
  }
}
```

## Domain Assembly Registration

```csharp
// In AssemblyInfo.cs or GlobalUsings.cs
[assembly: DomainAssembly(typeof(Order), typeof(Customer), typeof(Product))]

// This enables automatic discovery of:
// - Command/Query handlers in the Order, Customer, Product assemblies
// - FluentValidation validators
// - Integration events
```

## Domain Assembly Configuration

```csharp
// In your API project's GlobalUsings.cs or AssemblyInfo.cs
[assembly: DomainAssembly(typeof(Order), typeof(Customer))]

// This will scan Order and Customer assemblies for validators like:
// Orders.Domain.Commands.CreateOrderValidator
// Customers.Domain.Queries.GetCustomerValidator
```

## Example Validator in Domain Assembly

```csharp
// In Orders.Domain assembly
public class CreateOrderValidator : AbstractValidator<CreateOrder>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");
            
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");
            
        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator());
    }
}
```

## Automatic Integration with Wolverine

```csharp
// Validation happens automatically in message handlers
public static async Task<OrderCreated> Handle(
    CreateOrder command,  // Automatically validated
    IOrderRepository orders,
    ILogger logger)
{
    // Command is guaranteed to be valid when this executes
    // ValidationException is thrown automatically if invalid
    
    var order = new Order(command.CustomerId, command.Items);
    await orders.SaveAsync(order);
    
    return new OrderCreated(order.Id, order.CustomerId);
}
```

## Standard Application Startup

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder.Build();
app.MapDefaultHealthCheckEndpoints();
app.MapOrdersApi();

// This handles both normal execution and CLI commands
await app.RunAsync(args);
```

## Database Migration in Docker

```dockerfile
# Dockerfile for migration container
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY publish/ /app
WORKDIR /app
ENTRYPOINT ["dotnet", "OrderService.dll", "db-apply"]
```

## Health Check Validation

```bash
# In CI/CD pipeline or health monitoring
docker run --rm order-service:latest check-env

# Returns exit code 0 if healthy, non-zero if issues detected
```