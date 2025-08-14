---
title: Getting Started with Momentum Libraries
description: Learn how to use Momentum Libraries to build production-ready .NET 9 microservices with minimal ceremony and maximum productivity. Includes service defaults, extensions, source generators, and messaging infrastructure.
date: 2025-01-15
---

# Getting Started with Momentum

Welcome to **Momentum Libraries** - a collection of .NET 9 libraries that provide platform services, extensions, and source generators for building production-ready microservices. Whether you're creating new applications or enhancing existing ones, these libraries offer battle-tested patterns that reduce boilerplate and accelerate development.

## Overview

**Momentum Libraries** provide essential building blocks for modern .NET applications:

-   **🚀 Minimal Setup**: Get productive in minutes with comprehensive service defaults
-   **🔧 Production-Ready**: Battle-tested patterns used in high-scale applications
-   **⚡ Modern Stack**: Built for .NET 9 with Aspire, OpenTelemetry, and async-first design
-   **🎯 Focused Libraries**: Use only what you need - each library has a specific purpose
-   **📖 Excellent Documentation**: Clear examples and comprehensive guides

## Why Choose Momentum Libraries?

### **Template-Independent Usage**

Unlike the full Momentum template system, these libraries work with **any .NET 9 application**. Add them incrementally to existing projects or use them as the foundation for new ones.

### **Modern Development Patterns**

-   **Result Types**: Elegant error handling without exceptions
-   **CQRS Abstractions**: Clean command/query separation
-   **Source Generation**: Compile-time code generation for database commands
-   **Observability**: Built-in OpenTelemetry, Serilog, and health checks
-   **Event-Driven**: Kafka integration with CloudEvents standard

### **Developer Experience**

-   **IntelliSense Support**: Full IDE integration with source generators
-   **Minimal Configuration**: Sensible defaults that just work
-   **Extensible**: Configure and customize to fit your needs
-   **Testing-Friendly**: Designed for easy unit and integration testing

## Prerequisites

Before getting started, ensure you have:

-   **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
-   **IDE**: Visual Studio 2022 17.8+, VS Code with C# Dev Kit, or JetBrains Rider
-   **Docker Desktop** (optional) - For running databases and Kafka locally

## Quick Start (5 Minutes)

Let's build a simple API service using Momentum Libraries. This example demonstrates the core concepts and shows you how the libraries work together.

### 1. Create New Project

```bash
# Create a new ASP.NET Core API project
dotnet new webapi -n OrderService
cd OrderService

# Add the essential Momentum packages
dotnet add package Momentum.ServiceDefaults --version 0.0.1
dotnet add package Momentum.Extensions --version 0.0.1
```

### 2. Configure Service Defaults

Replace the content of `Program.cs`:

```csharp
using Momentum.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Momentum service defaults (observability, health checks, validation)
builder.AddServiceDefaults();

// Add your application services
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapDefaultEndpoints(); // Health checks, metrics

// Add your API endpoints
app.MapPost("/orders", async (CreateOrderCommand command, IOrderService orderService) =>
{
    var result = await orderService.CreateOrderAsync(command);

    return result.IsSuccess
        ? Results.Created($"/orders/{result.Value.Id}", result.Value)
        : Results.BadRequest(result.Errors);
});

app.MapGet("/orders/{id:guid}", async (Guid id, IOrderService orderService) =>
{
    var result = await orderService.GetOrderAsync(id);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.NotFound();
});

await app.RunAsync();
```

### 3. Define Commands and Models

Create `Models/Order.cs`:

```csharp
namespace OrderService.Models;

public record Order(
    Guid Id,
    string CustomerName,
    string ProductName,
    decimal Amount,
    DateTime CreatedAt
);

public record CreateOrderCommand(
    string CustomerName,
    string ProductName,
    decimal Amount
);
```

### 4. Implement Service with Result Types

Create `Services/IOrderService.cs`:

```csharp
using Momentum.Extensions;
using OrderService.Models;

namespace OrderService.Services;

public interface IOrderService
{
    Task<Result<Order>> CreateOrderAsync(CreateOrderCommand command);
    Task<Result<Order>> GetOrderAsync(Guid id);
}
```

Create `Services/OrderService.cs`:

```csharp
using Momentum.Extensions;
using OrderService.Models;
using FluentValidation.Results;

namespace OrderService.Services;

public class OrderService : IOrderService
{
    private static readonly Dictionary<Guid, Order> _orders = new();

    public Task<Result<Order>> CreateOrderAsync(CreateOrderCommand command)
    {
        // Validate input (in real apps, use FluentValidation)
        if (string.IsNullOrWhiteSpace(command.CustomerName))
        {
            var errors = new List<ValidationFailure>
            {
                new("CustomerName", "Customer name is required")
            };
            return Task.FromResult(Result<Order>.Failure(errors));
        }

        if (command.Amount <= 0)
        {
            var errors = new List<ValidationFailure>
            {
                new("Amount", "Amount must be greater than zero")
            };
            return Task.FromResult(Result<Order>.Failure(errors));
        }

        // Create the order
        var order = new Order(
            Id: Guid.CreateVersion7(),
            CustomerName: command.CustomerName,
            ProductName: command.ProductName,
            Amount: command.Amount,
            CreatedAt: DateTime.UtcNow
        );

        _orders[order.Id] = order;

        return Task.FromResult(Result<Order>.Success(order));
    }

    public Task<Result<Order>> GetOrderAsync(Guid id)
    {
        if (_orders.TryGetValue(id, out var order))
        {
            return Task.FromResult(Result<Order>.Success(order));
        }

        var errors = new List<ValidationFailure>
        {
            new("Id", "Order not found")
        };
        return Task.FromResult(Result<Order>.Failure(errors));
    }
}
```

### 5. Test Your Service

```bash
# Run the application
dotnet run

# The service starts with:
# - API endpoints: https://localhost:7001
# - Health check: https://localhost:7001/health
# - Metrics: https://localhost:7001/metrics

# Test creating an order
curl -X POST https://localhost:7001/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "John Doe",
    "productName": "Laptop",
    "amount": 1299.99
  }'

# Test retrieving an order (use the ID from the response above)
curl https://localhost:7001/orders/{order-id}

# Check health status
curl https://localhost:7001/health
```

**Congratulations!** You now have a working API with:

-   ✅ Structured error handling with Result types
-   ✅ Built-in health checks and metrics
-   ✅ OpenTelemetry observability
-   ✅ Structured logging with Serilog
-   ✅ Production-ready service defaults

## Understanding What Happened

In just a few minutes, you added powerful capabilities to your application:

### Service Defaults (`builder.AddServiceDefaults()`)

-   **Health Checks**: `/health` (detailed) and `/alive` (simple) endpoints
-   **OpenTelemetry**: Metrics, tracing, and logging correlation
-   **Serilog**: Structured logging with exception details
-   **Resilience**: HTTP client retry and circuit breaker patterns
-   **Service Discovery**: Automatic endpoint resolution in distributed systems

### Result Types (`Result<T>`)

-   **Error Handling**: No more try-catch blocks for business logic
-   **Type Safety**: Compile-time guarantees about success/failure states
-   **Validation**: Built-in support for FluentValidation results
-   **API Friendly**: Easy conversion to HTTP status codes

### Observability Out-of-the-Box

-   **Structured Logs**: JSON format with correlation IDs
-   **Metrics**: Application and infrastructure metrics
-   **Tracing**: Request tracking across service boundaries
-   **Health Monitoring**: Automated health check endpoints

## Core Libraries Introduction

Momentum Libraries are designed to work independently or together. Here's what each library provides:

### **Momentum.Extensions** - Core Foundation

Essential utilities and patterns for any .NET application.

```bash
dotnet add package Momentum.Extensions
```

**Key Features:**

-   **Result<T> Types**: Elegant error handling without exceptions
-   **Validation Integration**: FluentValidation helpers and extensions
-   **Data Access**: Enhanced Dapper extensions and LINQ2DB support
-   **Messaging Abstractions**: Base interfaces for CQRS and event-driven design

**Use When:** You want robust error handling and core utilities in any .NET project.

### **Momentum.ServiceDefaults** - Production Readiness

Complete service configuration for Aspire-based applications.

```bash
dotnet add package Momentum.ServiceDefaults
```

**Key Features:**

-   **Aspire Integration**: Full .NET Aspire service defaults implementation
-   **Observability Stack**: OpenTelemetry + Serilog for monitoring
-   **Health Checks**: Built-in application health monitoring
-   **Resilience**: HTTP client resilience patterns
-   **Service Discovery**: Automatic service resolution

**Use When:** Building microservices, APIs, or any distributed application that needs production-ready configuration.

### **Momentum.ServiceDefaults.Api** - API Enhancements

Additional features specifically for REST and gRPC APIs.

```bash
dotnet add package Momentum.ServiceDefaults.Api
```

**Key Features:**

-   **OpenAPI**: Enhanced Swagger documentation with XML docs
-   **gRPC Support**: Service registration and health checks
-   **Route Conventions**: Kebab-case URL transformations
-   **Response Types**: Automatic response type generation

**Use When:** Building REST APIs or gRPC services that need enhanced documentation and conventions.

### **Momentum.Extensions.SourceGenerators** - Code Generation

Compile-time code generation for common patterns.

```bash
dotnet add package Momentum.Extensions.SourceGenerators
```

**Key Features:**

-   **DbCommand Generation**: Type-safe database command handlers
-   **Zero Runtime Overhead**: All generation happens at compile time
-   **IDE Integration**: Generated code appears in IntelliSense
-   **Customizable**: Configure generation through attributes and MSBuild properties

**Use When:** You want to eliminate boilerplate code and ensure type safety for database operations.

### **Momentum.Extensions.Messaging.Kafka** - Event-Driven Architecture

Kafka integration with CloudEvents standard support.

```bash
dotnet add package Momentum.Extensions.Messaging.Kafka
```

**Key Features:**

-   **CloudEvents**: Standards-compliant event serialization
-   **Kafka Integration**: Producer and consumer patterns
-   **Partition Key Support**: Automatic partitioning strategies
-   **Observability**: Built-in metrics and tracing

**Use When:** Building event-driven microservices that need reliable messaging.

## Your First Real Application

Let's build a more complete example that demonstrates how the libraries work together. We'll create an e-commerce order service with database persistence, validation, and events.

### 1. Setup Project

```bash
# Create new project
dotnet new webapi -n EcommerceService
cd EcommerceService

# Add comprehensive Momentum packages
dotnet add package Momentum.ServiceDefaults
dotnet add package Momentum.Extensions
dotnet add package Momentum.Extensions.SourceGenerators
dotnet add package Npgsql  # For PostgreSQL
dotnet add package FluentValidation.DependencyInjectionExtensions
```

### 2. Database Models and Commands

Create `Domain/Orders/Order.cs`:

```csharp
namespace EcommerceService.Domain.Orders;

// Database entity
public class OrderEntity
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Public model
public record Order(
    Guid Id,
    string CustomerName,
    string ProductName,
    decimal Amount,
    DateTime CreatedAt
);

// Extension for conversion
public static class OrderExtensions
{
    public static Order ToModel(this OrderEntity entity) => new(
        entity.Id,
        entity.CustomerName,
        entity.ProductName,
        entity.Amount,
        entity.CreatedAt
    );
}
```

### 3. Commands with Validation

Create `Domain/Orders/Commands/CreateOrder.cs`:

```csharp
using FluentValidation;
using Momentum.Extensions;

namespace EcommerceService.Domain.Orders.Commands;

public record CreateOrderCommand(
    string CustomerName,
    string ProductName,
    decimal Amount
) : ICommand<Result<Order>>;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage("Customer name is required")
            .MinimumLength(2)
            .WithMessage("Customer name must be at least 2 characters")
            .MaximumLength(100)
            .WithMessage("Customer name cannot exceed 100 characters");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Amount cannot exceed $1,000,000");
    }
}
```

### 4. Database Commands with Source Generation

Create `Domain/Orders/Data/OrderDbCommands.cs`:

```csharp
using Dapper;
using Momentum.Extensions.Abstractions.Dapper;
using System.Data;

namespace EcommerceService.Domain.Orders.Data;

public static class CreateOrderDbCommandHandler
{
    [DbCommand]
    public record DbCommand(OrderEntity Order) : ICommand<OrderEntity>;

    public static async Task<OrderEntity> Handle(
        DbCommand command,
        IDbConnection db,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO orders (id, customer_name, product_name, amount, created_at, updated_at)
            VALUES (@Id, @CustomerName, @ProductName, @Amount, @CreatedAt, @UpdatedAt)
            RETURNING *;
            """;

        var order = command.Order;
        return await db.QuerySingleAsync<OrderEntity>(sql, new
        {
            order.Id,
            order.CustomerName,
            order.ProductName,
            order.Amount,
            order.CreatedAt,
            order.UpdatedAt
        });
    }
}

public static class GetOrderDbCommandHandler
{
    [DbCommand]
    public record DbCommand(Guid OrderId) : ICommand<OrderEntity?>;

    public static async Task<OrderEntity?> Handle(
        DbCommand command,
        IDbConnection db,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT * FROM orders WHERE id = @OrderId";
        return await db.QuerySingleOrDefaultAsync<OrderEntity>(sql, new { command.OrderId });
    }
}
```

### 5. Business Logic Handler

Create `Domain/Orders/Commands/CreateOrderHandler.cs`:

```csharp
using EcommerceService.Domain.Orders.Data;
using FluentValidation;
using Momentum.Extensions;

namespace EcommerceService.Domain.Orders.Commands;

public static class CreateOrderCommandHandler
{
    public static async Task<Result<Order>> Handle(
        CreateOrderCommand command,
        IValidator<CreateOrderCommand> validator,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        // Validate input
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<Order>.Failure(validationResult.Errors);
        }

        // Create database entity
        var orderEntity = new OrderEntity
        {
            Id = Guid.CreateVersion7(),
            CustomerName = command.CustomerName,
            ProductName = command.ProductName,
            Amount = command.Amount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Save to database using generated command
        var dbCommand = new CreateOrderDbCommandHandler.DbCommand(orderEntity);
        var savedOrder = await messageBus.InvokeAsync(dbCommand, cancellationToken);

        return Result<Order>.Success(savedOrder.ToModel());
    }
}
```

### 6. API Configuration

Update `Program.cs`:

```csharp
using EcommerceService.Domain.Orders.Commands;
using FluentValidation;
using Momentum.Extensions;
using Npgsql;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add Momentum service defaults
builder.AddServiceDefaults();

// Add database connection
builder.Services.AddScoped<IDbConnection>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=ecommerce;Username=postgres;Password=password";
    return new NpgsqlConnection(connectionString);
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();

// Add custom services
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

// Configure pipeline
app.MapDefaultEndpoints();

// Add API endpoints
app.MapPost("/orders", async (
    CreateOrderCommand command,
    IValidator<CreateOrderCommand> validator,
    IMessageBus messageBus,
    CancellationToken cancellationToken) =>
{
    var result = await CreateOrderCommandHandler.Handle(command, validator, messageBus, cancellationToken);

    return result.IsSuccess
        ? Results.Created($"/orders/{result.Value.Id}", result.Value)
        : Results.BadRequest(result.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
});

app.MapGet("/orders/{id:guid}", async (
    Guid id,
    IMessageBus messageBus,
    CancellationToken cancellationToken) =>
{
    var dbCommand = new GetOrderDbCommandHandler.DbCommand(id);
    var orderEntity = await messageBus.InvokeAsync(dbCommand, cancellationToken);

    return orderEntity is not null
        ? Results.Ok(orderEntity.ToModel())
        : Results.NotFound();
});

await app.RunAsync();
```

### 7. Database Setup

Create a simple migration script `setup.sql`:

```sql
-- Create orders table
CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY,
    customer_name VARCHAR(100) NOT NULL,
    product_name VARCHAR(200) NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Create index for performance
CREATE INDEX IF NOT EXISTS idx_orders_created_at ON orders(created_at);
```

### 8. Configuration

Create `appsettings.Development.json`:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Database=ecommerce;Username=postgres;Password=password"
    },
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    }
}
```

### 9. Run and Test

```bash
# Start PostgreSQL (using Docker)
docker run --name postgres-ecommerce -e POSTGRES_PASSWORD=password -e POSTGRES_DB=ecommerce -p 5432:5432 -d postgres:15

# Run the setup script
psql -h localhost -U postgres -d ecommerce -f setup.sql

# Start the application
dotnet run

# Test creating orders
curl -X POST https://localhost:7001/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Alice Johnson",
    "productName": "Gaming Laptop",
    "amount": 1899.99
  }'

# Test validation errors
curl -X POST https://localhost:7001/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "",
    "productName": "Invalid Product",
    "amount": -100
  }'
```

## Integration Patterns

### Combining Multiple Libraries

The power of Momentum Libraries comes from how they work together:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Foundation: Service defaults for observability and health
builder.AddServiceDefaults();

// API enhancements: OpenAPI, gRPC, route conventions
builder.AddApiDefaults();

// Extensions: Result types, validation, data access
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Source generators: Automatic DbCommand handling
// (Automatically activated when package is referenced)

// Messaging: Event-driven capabilities
builder.AddKafkaMessaging(builder.Configuration);

var app = builder.Build();

// All the endpoints and middleware are configured
app.MapDefaultEndpoints();
app.MapApiEndpoints();

await app.RunAsync();
```

### Error Handling Patterns

Consistent error handling across your application:

```csharp
// Service layer
public async Task<Result<Customer>> GetCustomerAsync(Guid id)
{
    var customer = await customerRepository.GetByIdAsync(id);

    return customer switch
    {
        null => Result<Customer>.NotFound("Customer", id.ToString()),
        _ => Result<Customer>.Success(customer)
    };
}

// API layer
app.MapGet("/customers/{id:guid}", async (Guid id, ICustomerService service) =>
{
    var result = await service.GetCustomerAsync(id);

    return result.Match(
        onSuccess: customer => Results.Ok(customer),
        onFailure: errors => Results.BadRequest(errors)
    );
});
```

### Event-Driven Integration

Publish and consume events across services:

```csharp
// Domain event
[EventTopic("ecommerce.orders.order-created")]
public record OrderCreated(
    [PartitionKey] Guid CustomerId,
    Order Order
);

// Command handler that publishes events
public static async Task<(Result<Order>, OrderCreated?)> Handle(
    CreateOrderCommand command,
    IMessageBus messageBus)
{
    // Business logic...
    var order = await CreateOrderInDatabase(command);

    // Create integration event
    var orderCreated = new OrderCreated(order.CustomerId, order);

    return (Result<Order>.Success(order), orderCreated);
}

// Event handler in another service
public class OrderCreatedHandler
{
    public async Task Handle(OrderCreated orderCreated, CancellationToken cancellationToken)
    {
        // Process the order creation in inventory service
        await inventoryService.ReserveItemsAsync(orderCreated.Order.Items, cancellationToken);
    }
}
```

## Best Practices

### 1. **Start Small, Scale Up**

```csharp
// Begin with essentials
builder.AddServiceDefaults();
builder.Services.AddScoped<IMyService, MyService>();

// Add capabilities as needed
builder.AddApiDefaults();        // When you need enhanced APIs
builder.AddKafkaMessaging();     // When you need events
// Source generators activate automatically
```

### 2. **Consistent Error Handling**

```csharp
// Always use Result<T> for business operations
public async Task<Result<Customer>> CreateCustomerAsync(CreateCustomerCommand command)
{
    // Validation
    var validationResult = await validator.ValidateAsync(command);
    if (!validationResult.IsValid)
        return Result<Customer>.Failure(validationResult.Errors);

    // Business logic
    try
    {
        var customer = await customerRepository.CreateAsync(command.ToEntity());
        return Result<Customer>.Success(customer.ToModel());
    }
    catch (Exception ex) when (ex is not ValidationException)
    {
        logger.LogError(ex, "Failed to create customer");
        return Result<Customer>.Failure("An error occurred while creating the customer");
    }
}
```

### 3. **Leverage Source Generation**

```csharp
// Mark database commands for generation
[DbCommand]
public record GetCustomersQuery(int Page, int PageSize) : IQuery<IEnumerable<Customer>>;

// The source generator creates the handler automatically
// Use dependency injection to access the generated handler
```

### 4. **Configuration Patterns**

```csharp
// Use configuration sections for complex settings
builder.Services.Configure<OrderServiceOptions>(
    builder.Configuration.GetSection("OrderService"));

// Validate configuration at startup
builder.Services.AddOptions<OrderServiceOptions>()
    .Bind(builder.Configuration.GetSection("OrderService"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## Next Steps

Now that you've built your first Momentum Libraries application, explore these advanced topics:

### **Essential Reading**

1. **[Service Configuration Guide](./service-configuration/)** - Deep dive into observability, health checks, and resilience
2. **[Error Handling Patterns](./error-handling)** - Master the Result pattern and validation
3. **[Database Operations](./database/)** - Learn DbCommand source generation and best practices

### **Advanced Integration**

4. **[Event-Driven Messaging](./messaging/)** - Build robust event-driven architectures with Kafka
5. **[Testing Strategies](./testing/)** - Comprehensive testing patterns for Momentum applications
6. **[Best Practices](./best-practices)** - Production-ready patterns and guidelines

### **Specialized Topics**

7. **[CQRS Implementation](./cqrs/)** - Command/Query separation patterns
8. **[Architecture Decisions](./arch/)** - Design patterns and architectural guidance
9. **[Troubleshooting](./troubleshooting)** - Common issues and solutions

### **Community and Support**

-   **API Reference**: Browse the complete [API documentation](/reference/Momentum)
-   **Sample Applications**: See [real-world examples](https://github.com/vgmello/momentum/tree/main/examples)
-   **GitHub Discussions**: Ask questions and share experiences
-   **Contributing**: Help improve the libraries for everyone

## Common Patterns Quick Reference

### Result Type Usage

```csharp
// Success
return Result<Customer>.Success(customer);

// Single error
return Result<Customer>.Failure("Customer not found");

// Multiple errors (validation)
return Result<Customer>.Failure(validationResult.Errors);

// Chaining operations
var result = await GetCustomerAsync(id);
if (result.IsFailure) return result;

return await UpdateCustomerAsync(result.Value);
```

### Service Registration

```csharp
// Essential services
builder.AddServiceDefaults();

// API services
builder.AddApiDefaults();

// Database
builder.Services.AddScoped<IDbConnection>(_ =>
    new NpgsqlConnection(connectionString));

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Custom services
builder.Services.AddScoped<IOrderService, OrderService>();
```

### API Endpoint Patterns

```csharp
// Command endpoint with validation
app.MapPost("/orders", async (CreateOrderCommand command, IMessageBus bus) =>
{
    var result = await bus.InvokeAsync(command);
    return result.IsSuccess
        ? Results.Created($"/orders/{result.Value.Id}", result.Value)
        : Results.BadRequest(result.Errors);
});

// Query endpoint
app.MapGet("/orders/{id:guid}", async (Guid id, IMessageBus bus) =>
{
    var query = new GetOrderQuery(id);
    var result = await bus.InvokeAsync(query);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
});
```

**Ready to build amazing applications?** Choose your path:

-   **[Service Configuration](./service-configuration/)** - Configure observability and infrastructure
-   **[Database Integration](./database/)** - Add database operations with source generation
-   **[Event Messaging](./messaging/)** - Build event-driven microservices
-   **[API Reference](/reference/Momentum)** - Explore the complete API surface

---

_Momentum Libraries: Minimal ceremony, maximum productivity. Build better services faster._
