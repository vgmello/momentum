# Momentum .NET

[![NuGet](https://img.shields.io/nuget/v/Momentum.Extensions.Abstractions?style=flat-square&color=green)](https://www.nuget.org/packages/Momentum.Extensions.Abstractions)
[![NuGet Preview](https://img.shields.io/nuget/vpre/Momentum.Extensions.Abstractions?style=flat-square&label=nuget-preview)](https://www.nuget.org/packages/Momentum.Extensions.Abstractions)
[![Downloads](https://img.shields.io/nuget/dt/Momentum.Extensions.Abstractions?style=flat-square)](https://www.nuget.org/packages/Momentum.Extensions.Abstractions)
[![License](https://img.shields.io/github/license/vgmello/momentum?style=flat-square)](https://github.com/vgmello/momentum/blob/main/LICENSE)

Welcome to **Momentum** - a comprehensive .NET 10 template system that generates complete, production-ready microservices solutions. Whether you're building APIs, event-driven backends, or stateful processing systems, Momentum provides the architecture, patterns, and infrastructure you need to get productive immediately.

## Quick Start (2 Minutes)

Get a complete microservices solution running in under 2 minutes:

### 1. Install the Template

```bash
# Install from NuGet (recommended)
dotnet new install Momentum.Template
```

### 2. Generate Your Solution

```bash
# Create a complete microservices solution
dotnet new mmt -n OrderService --allow-scripts yes
cd OrderService
```

### 3. Start Everything with Aspire

```bash
# Launch the complete application stack
dotnet run --project src/OrderService.AppHost

# Access the Aspire Dashboard: https://localhost:18110
# API endpoints: http://localhost:8101 (REST), http://localhost:8102 (gRPC)
# Documentation: http://localhost:8119
```

**That's it!** You now have a running microservices solution with:

- ✅ REST and gRPC APIs with sample endpoints
- ✅ Background event processing with Wolverine
- ✅ PostgreSQL database with migrations
- ✅ Apache Kafka messaging
- ✅ Comprehensive observability
- ✅ Live documentation
- ✅ Sample business domain (Cashiers/Invoices)

## Technology Stack

Momentum is built on modern, production-proven technologies:

- **🎯 .NET 10**: Latest framework with performance optimizations
- **🏗️ .NET Aspire**: Local development orchestration and observability
- **🎭 Orleans**: Stateful actor-based processing for complex workflows
- **⚡ Wolverine**: CQRS/MediatR pattern with message handling
- **🚀 gRPC + REST**: Dual API protocols for performance and compatibility
- **📡 Apache Kafka**: Event streaming and reliable messaging
- **🗄️ PostgreSQL**: Robust relational database with JSON support
- **🔄 Liquibase**: Version-controlled database migrations
- **📊 OpenTelemetry**: Distributed tracing and observability
- **🔌 Refit + Refitter**: Type-safe REST API clients with OpenAPI code generation
- **🧪 Testcontainers**: Real infrastructure for integration testing

## Template System Overview

**Momentum Template** (`dotnet new mmt`) generates customized microservices solutions that mirror real-world business operations. The template leverages Wolverine for CQRS patterns and supports multiple architectures from simple APIs to complex event-driven systems with Orleans actors.

**Architecture style**: Domain-Oriented Vertical Slice Architecture (CQRS + Event-Driven), using DDD-inspired boundaries (bounded contexts, domain language, contracts/events) with a pragmatic application-service style.

### **Core Architecture Principles**

- **🎯 Real-World Mirroring**: Code structure corresponds to business operations and processes
- **🚫 No Smart Objects**: Entities are data records, not self-modifying objects
- **🏢 Front/Back Office**: Synchronous APIs vs Asynchronous processing
- **📡 Event-Driven**: Integration events via Kafka with Wolverine message handling
- **🧭 Pragmatic Domain Boundaries**: Bounded contexts, domain language, and contracts/events without heavy domain object ceremony
- **🧪 Testing First**: Comprehensive testing with real infrastructure

## Prerequisites

Before getting started, ensure you have:

- **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **IDE**: Visual Studio, VS Code with C# Dev Kit, or JetBrains Rider
- **Local Container Solution (Docker, Rancher, Podman, etc)** - Required for databases, Kafka, and local development

## Template Configuration Options

The template supports extensive customization through parameters. Here are the most common configurations:

### **Simple API Service**

```bash
# Generate API-only service
dotnet new mmt -n PaymentService --api --backoffice false --orleans false --docs false
```

### **Orleans Processing Engine**

```bash
# Generate stateful processing service
dotnet new mmt -n WorkflowEngine --orleans --api false --port 9000
```

### **Full Stack Solution**

```bash
# Generate complete solution with custom settings
dotnet new mmt -n EcommercePlatform --org "Acme Corp" --port 7000
```

### **Minimal Setup**

```bash
# Clean slate without sample code
dotnet new mmt -n CleanService --no-sample
```

### **Available Template Parameters**

The template offers comprehensive configuration options:

**Core Components:**

- `--api`: REST/gRPC API project (default: true)
- `--backoffice`: Background processing project (default: true)
- `--aspire`: .NET Aspire orchestration project (default: true)
- `--docs`: VitePress documentation project (default: true)
- `--orleans`: Orleans stateful processing project (default: false)

**Infrastructure:**

- `--kafka`: Apache Kafka messaging (default: true)
- `--db-config`: Database setup (`default`, `npgsql`, `liquibase`, `none`)
- `--port`: Base port number (default: 8100)

**Customization:**

- `--org`: Organization name for copyright headers, github, etc
- `--no-sample`: Exclude sample code (default: false, use `--no-sample` to skip)
- `--project-only`: Generate only projects without solution files
- `--libs`: Include Momentum libraries as project references
- `--lib-name`: Custom prefix to replace "Momentum" in library names
- `--local`: Use local Momentum packages from `libs/Momentum/.local/nuget` (template development)

> [!NOTE]
> For complete parameter documentation and all available combinations, see the [`template.json`](.template.config/template.json) file and the [Template Options Guide](https://momentumlib.net/guide/template-options/) for detailed use cases and examples.

## Understanding the Generated Solution

The template generates a production-ready solution with clear separation of concerns:

### **Project Structure**

```
OrderService/
├── src/
│   ├── OrderService.Api/              # REST & gRPC endpoints
│   ├── OrderService.BackOffice/        # Event processing
│   ├── OrderService.BackOffice.Orleans/ # Stateful processing (if enabled)
│   ├── OrderService.AppHost/           # Aspire orchestration
│   ├── OrderService/                   # Core domain logic
│   └── OrderService.Contracts/         # Integration events
├── infra/
│   └── OrderService.Database/          # Liquibase migrations
├── tests/
│   └── OrderService.Tests/             # Comprehensive testing
├── docs/                               # VitePress documentation
└── compose.yml                         # Docker Compose for services
```

### **Business Domain Organization**

Generated code follows a Domain-Oriented Vertical Slice approach with Wolverine CQRS patterns and event-driven contracts:

```
src/OrderService/
├── Cashiers/                  # Sample business domain
│   ├── Commands/              # Business actions (Wolverine handlers)
│   ├── Queries/               # Information retrieval (Wolverine handlers)
│   ├── Data/                  # Database operations
│   └── Contracts/             # Domain events
└── Invoices/                  # Another sample domain
    ├── Commands/              # Commands with validation
    ├── Queries/               # Queries for data retrieval
    ├── Data/                  # Database access layer
    └── Contracts/             # Integration events
```

### **Port Configuration**

Services use a base port system (default: 8100):

| Service          | HTTP  | HTTPS | gRPC | Purpose               |
| ---------------- | ----- | ----- | ---- | --------------------- |
| Aspire Dashboard | 18100 | 18110 | -    | Development dashboard |
| API              | 8101  | 8111  | 8102 | REST & gRPC endpoints |
| BackOffice       | 8103  | 8113  | -    | Background processing |
| Orleans          | 8104  | 8114  | -    | Stateful processing   |
| Documentation    | 8119  | -     | -    | VitePress docs        |
| PostgreSQL       | 54320 | -     | -    | Database              |
| Kafka            | 59092 | -     | -    | Message broker        |

Customize the base port with `--port 9000` to use 9100, 9101, etc.

## Individual Libraries Approach

For existing projects or custom architectures, you can use Momentum Libraries individually. All these capabilities can also be configured through the template system with selective feature inclusion:

📚 **Note**: The template system (`dotnet new mmt`) allows you to configure which libraries and features to include, so you can generate solutions with only the specific Momentum capabilities you need.

Build a simple API service using individual Momentum Libraries when you need to add capabilities to existing projects:

### 1. Create New Project

```bash
# Create a new ASP.NET Core API project
dotnet new webapi -n OrderService
cd OrderService

# Add the essential Momentum packages
dotnet add package Momentum.ServiceDefaults
dotnet add package Momentum.ServiceDefaults.Api
dotnet add package Momentum.Extensions
```

## Library Integration Results

Congratulations! You've added powerful capabilities to your application:

- ✅ Structured error handling with Result types
- ✅ Built-in health checks and metrics
- ✅ OpenTelemetry observability
- ✅ Structured logging with Serilog
- ✅ Production-ready service defaults

## Individual Libraries Overview

When using libraries individually (rather than the template), each library serves a specific purpose and can be used independently:

### **Momentum.Extensions** - Core Foundation

Essential utilities and patterns for any .NET application.

```bash
dotnet add package Momentum.Extensions
```

**Key Features:**

- **ResultOfT Types**: Elegant error handling without exceptions
- **Validation Integration**: FluentValidation helpers and extensions
- **Data Access**: Enhanced Dapper extensions and LINQ2DB support
- **Messaging Abstractions**: Base interfaces for CQRS and event-driven design with Wolverine integration

**Use When:** You want robust error handling and core utilities in any .NET project.

### **Momentum.ServiceDefaults** - Production Readiness

Complete service configuration for Aspire-based applications.

```bash
dotnet add package Momentum.ServiceDefaults
```

**Key Features:**

- **Aspire Integration**: Full .NET Aspire service defaults implementation
- **Observability Stack**: OpenTelemetry + Serilog for monitoring
- **Health Checks**: Built-in application health monitoring
- **Resilience**: HTTP client resilience patterns
- **Service Discovery**: Automatic service resolution

**Use When:** Building microservices, APIs, or any distributed application that needs production-ready configuration.

### **Momentum.ServiceDefaults.Api** - API Enhancements

Additional features specifically for REST and gRPC APIs.

```bash
dotnet add package Momentum.ServiceDefaults.Api
```

**Key Features:**

- **OpenAPI**: Enhanced Swagger documentation with XML docs
- **gRPC Support**: Service registration and health checks
- **Route Conventions**: Kebab-case URL transformations
- **Response Types**: Automatic response type generation

**Use When:** Building REST APIs or gRPC services that need enhanced documentation and conventions.

### **Momentum.Extensions.SourceGenerators** - Code Generation

Compile-time code generation for common patterns.

```bash
dotnet add package Momentum.Extensions.SourceGenerators
```

**Key Features:**

- **DbCommand Generation**: Type-safe database command handlers
- **Zero Runtime Overhead**: All generation happens at compile time
- **IDE Integration**: Generated code appears in IntelliSense
- **Customizable**: Configure generation through attributes and MSBuild properties

**Use When:** You want to eliminate boilerplate code and ensure type safety for database operations.

### **Momentum.Extensions.Messaging.Kafka** - Event-Driven Architecture

Kafka integration with CloudEvents standard support.

```bash
dotnet add package Momentum.Extensions.Messaging.Kafka
```

**Key Features:**

- **CloudEvents**: Standards-compliant event serialization
- **Kafka Integration**: Producer and consumer patterns
- **Partition Key Support**: Automatic partitioning strategies
- **Observability**: Built-in metrics and tracing

**Use When:** Building event-driven microservices that need reliable messaging.

## Advanced Library Integration Example

When using individual libraries, here's how to build a more complete application that demonstrates library integration:

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
using Momentum.Extensions.Abstractions.Messaging;
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
using Momentum.Extensions.Abstractions.Messaging;
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
            return validationResult.Errors;
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
        var savedOrder = await messageBus.InvokeCommandAsync(dbCommand, cancellationToken);

        return savedOrder.ToModel();
    }
}
```

### 6. API Configuration

Update `Program.cs`:

```csharp
using EcommerceService.Domain.Orders.Commands;
using FluentValidation;
using Momentum.Extensions;
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;
using Momentum.ServiceDefaults.Api.OpenApi.Extensions;
using Momentum.ServiceDefaults.HealthChecks;
using Npgsql;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add Momentum service defaults
builder.AddServiceDefaults();
builder.AddApiServiceDefaults(requireAuth: false);
builder.Services.AddOpenApi(options => options.ConfigureOpenApiDefaults());

// Add database connection
builder.Services.AddScoped<IDbConnection>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=ecommerce;Username=postgres;Password=password";
    return new NpgsqlConnection(connectionString);
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();

var app = builder.Build();

// Configure pipeline
app.ConfigureApiUsingDefaults();
app.MapDefaultHealthCheckEndpoints();

// Add API endpoints
app.MapPost("/orders", async (
    CreateOrderCommand command,
    IValidator<CreateOrderCommand> validator,
    IMessageBus messageBus,
    CancellationToken cancellationToken) =>
{
    var result = await CreateOrderCommandHandler.Handle(command, validator, messageBus, cancellationToken);

    return result.Match(
        onSuccess: order => Results.Created($"/orders/{order.Id}", order),
        onFailure: errors => Results.BadRequest(errors.Select(e => new { e.PropertyName, e.ErrorMessage }))
    );
});

app.MapGet("/orders/{id:guid}", async (
    Guid id,
    IMessageBus messageBus,
    CancellationToken cancellationToken) =>
{
    var dbCommand = new GetOrderDbCommandHandler.DbCommand(id);
    var orderEntity = await messageBus.InvokeCommandAsync(dbCommand, cancellationToken);

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
builder.AddApiServiceDefaults(requireAuth: false);
builder.Services.AddOpenApi(options => options.ConfigureOpenApiDefaults());

// Extensions: Result types, validation, data access
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Source generators: Automatic DbCommand handling
// (Automatically activated when package is referenced)

// Messaging: Event-driven capabilities
builder.AddKafkaMessagingExtensions();

var app = builder.Build();

// All the endpoints and middleware are configured
app.ConfigureApiUsingDefaults();
app.MapDefaultHealthCheckEndpoints();

await app.RunAsync();
```

### Error Handling Patterns

Consistent error handling across your application:

```csharp
// Service layer
public async Task<Result<Customer>> GetCustomerAsync(Guid id)
{
    var customer = await customerRepository.GetByIdAsync(id);

    return customer is null
        ? [new ValidationFailure(nameof(id), "Customer not found")]
        : customer;
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
    [PartitionKey] Guid OrderId,
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
    var orderCreated = new OrderCreated(order.Id, order);

    return (order, orderCreated);
}

// Event handler in another service
public class OrderCreatedHandler
{
    public async Task Handle(OrderCreated orderCreated, CancellationToken cancellationToken)
    {
        // Process the order creation in another bounded context
        await analyticsService.TrackOrderCreatedAsync(orderCreated.Order, cancellationToken);
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
builder.AddApiServiceDefaults(requireAuth: false); // When you need enhanced APIs
builder.AddKafkaMessagingExtensions();             // When you need events
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
        return validationResult.Errors;

    // Business logic
    try
    {
        var customer = await customerRepository.CreateAsync(command.ToEntity());
        return customer.ToModel();
    }
    catch (Exception ex) when (ex is not ValidationException)
    {
        logger.LogError(ex, "Failed to create customer");
        return [new ValidationFailure(string.Empty, "An error occurred while creating the customer")];
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

Choose your path based on how you're using Momentum:

### **If You Used the Template**

1. **[Explore the Generated Solution](https://momentumlib.net/guide/template-walkthrough/)** - Understand what was created
2. **[Add Your Business Domain](https://momentumlib.net/guide/adding-domains/)** - Replace sample code with your logic
3. **Deploy to Production** - Deploy your microservices (coming soon)
4. **[Template Options Reference](https://momentumlib.net/guide/template-options/)** - Complete parameter guide

### **If You're Using Individual Libraries**

1. **[Service Configuration Guide](https://momentumlib.net/guide/service-configuration/)** - Deep dive into observability, health checks, and resilience
2. **[Error Handling Patterns](https://momentumlib.net/guide/error-handling)** - Master the Result pattern and validation
3. **[Database Operations](https://momentumlib.net/guide/database/)** - Learn DbCommand source generation and best practices
4. **[Event-Driven Messaging](https://momentumlib.net/guide/messaging/)** - Build robust event-driven architectures with Kafka
5. **[Testing Strategies](https://momentumlib.net/guide/testing/)** - Comprehensive testing patterns for Momentum applications

### **Advanced Topics for Both Approaches**

6. **[CQRS Implementation](https://momentumlib.net/guide/cqrs/)** - Command/Query separation patterns
7. **[Architecture Decisions](https://momentumlib.net/guide/arch/)** - Design patterns and architectural guidance
8. **[Best Practices](https://momentumlib.net/guide/best-practices)** - Production-ready patterns and guidelines
9. **[Troubleshooting](https://momentumlib.net/guide/troubleshooting)** - Common issues and solutions

## Coming Soon

We're continuously expanding Momentum to include even more production-ready technologies and patterns:

- **SQL Server** - Additional database provider support alongside PostgreSQL
- **k6 for Performance Testing** - Automated load testing and performance validation
- **LGTM stack for improved observability** - Enhanced monitoring with Loki, Grafana, Tempo, and Mimir
- **Maybe REDIS** - Caching and session management capabilities

## Support

If you encounter any issues or require assistance, please [open an issue](https://github.com/vgmello/momentum/issues) on the project's GitHub page.

## Contribution

Momentum is designed to be copied and customized, If you've created patterns or improvements that would benefit the broader community, share it, contributions to the template are welcome!

## Code of Conduct

[![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-3.0-4baaaa.svg)](https://www.contributor-covenant.org/version/3/0/code_of_conduct/)

This project has adopted the code of conduct defined by the [Contributor Covenant](https://www.contributor-covenant.org/) to clarify expected behavior in our community. For more information see the [Code of Conduct](CODE_OF_CONDUCT.MD).

## Credits and Acknowledgements

Momentum makes use of several outstanding open-source libraries and frameworks. We extend our gratitude to the developers and contributors of these projects:

### Core Framework Libraries

- **[Wolverine](https://wolverinefx.io/)**: Next-generation message bus and CQRS framework for .NET, providing elegant command/query handling and message processing capabilities.
- **[Mapperly](https://github.com/riok/mapperly)**: A .NET source generator for generating object mappings at compile-time, offering zero-overhead and type-safe mapping.
- **[Microsoft Orleans](https://dotnet.github.io/orleans/)**: A cross-platform framework for building robust, scalable distributed applications with virtual actors.
- **[.NET Aspire](https://github.com/dotnet/aspire)**: An opinionated, cloud-ready stack for building observable, production-ready distributed applications.
- **[Asp.Versioning](https://github.com/dotnet/aspnet-api-versioning)**: API versioning for ASP.NET Core with support for URL, query string, and header-based versioning.

### Data & Messaging

- **[Dapper](https://github.com/DapperLib/Dapper)**: A simple object mapper for .NET with high performance and minimal overhead.
- **[linq2db](https://linq2db.github.io/)**: Fast, lightweight, and type-safe LINQ to SQL implementation for .NET.
- **[FluentValidation](https://fluentvalidation.net/)**: A popular .NET library for building strongly-typed validation rules.
- **[CloudNative CloudEvents](https://cloudevents.io/)**: A specification for describing event data in a common way with Kafka integration.
- **[Npgsql](https://www.npgsql.org/)**: The .NET data provider for PostgreSQL.
- **[Liquibase](https://www.liquibase.com/)**: Database schema change management and version control.

### API Client Generation

- **[Refit](https://github.com/reactiveui/refit)**: Automatic type-safe REST library for .NET that turns REST APIs into live interfaces.
- **[Refitter](https://github.com/christianhelle/refitter)**: OpenAPI client code generation via MSBuild, producing Refit interfaces and contracts from OpenAPI specifications at build time.

### Testing & Quality

- **[xUnit v3](https://xunit.net/)**: Modern, extensible testing framework for .NET applications.
- **[Shouldly](https://docs.shouldly.org/)**: Testing framework that focuses on giving great error messages when assertions fail.
- **[Testcontainers](https://dotnet.testcontainers.org/)**: A library to support tests with throwaway instances of Docker containers.
- **[NSubstitute](https://nsubstitute.github.io/)**: A friendly substitute for .NET mocking libraries.
- **[NetArchTest](https://github.com/BenMorris/NetArchTest)**: A fluent API for .NET that can enforce architectural rules in unit tests.
- **[Bogus](https://github.com/bchavez/Bogus)**: A simple fake data generator for .NET, useful for creating realistic test data.
- **[coverlet](https://github.com/coverlet-coverage/coverlet)**: Cross-platform code coverage library for .NET with support for line, branch, and method coverage.
- **[SonarAnalyzer](https://github.com/SonarSource/sonar-dotnet)**: Static code analysis for C# to detect bugs, vulnerabilities, and code smells.

### Observability & Infrastructure

- **[OpenTelemetry](https://opentelemetry.io/)**: A collection of tools, APIs, and SDKs for instrumenting, generating, collecting, and exporting telemetry data.
- **[Serilog](https://serilog.net/)**: Diagnostic logging library for .NET applications with rich structured event data.
- **[Scalar](https://github.com/scalar/scalar)**: Modern API documentation with interactive OpenAPI/Swagger support.
- **[gRPC](https://grpc.io/)**: A high-performance, open source universal RPC framework.
- **[Orleans Dashboard](https://github.com/OrleansContrib/OrleansDashboard)**: Web-based monitoring dashboard for Microsoft Orleans applications.

### Code Generation & Utilities

- **[OneOf](https://github.com/mcintyre321/OneOf)**: Discriminated unions for C# with exhaustive matching.
- **[Spectre.Console](https://spectreconsole.net/)**: A .NET library that makes it easier to create beautiful console applications.
- **[Fluid](https://github.com/sebastienros/fluid)**: High-performance, secure Liquid templating language for .NET.

Each of these libraries may be licensed differently, so we recommend you review their licenses if you plan to use Momentum in your own projects.

Special thanks to the .NET community and all contributors who help make these tools possible!

> This project only uses third-party libraries with permissive licenses (e.g., MIT, Apache 2.0) that are approved for commercial use.

---

_Momentum: Real-world microservices. Modern architecture. Production-ready from day one._
