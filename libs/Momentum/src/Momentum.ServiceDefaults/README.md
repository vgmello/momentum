# Momentum.ServiceDefaults

Service defaults for Momentum apps providing common configurations for Aspire-based services including Kafka, PostgreSQL, OpenTelemetry, Serilog, resilience patterns, and service discovery. Essential for all Momentum host projects.

## Overview

The `Momentum.ServiceDefaults` package provides a comprehensive set of default configurations for .NET Aspire-based services. It establishes consistent patterns for observability, messaging, data access, resilience, and service discovery across all Momentum applications.

## Installation

Add the package to your project using the .NET CLI:

```bash
dotnet add package Momentum.ServiceDefaults
```

Or using the Package Manager Console:

```powershell
Install-Package Momentum.ServiceDefaults
```

## Key Features

-   **Aspire Integration**: Complete .NET Aspire service defaults implementation
-   **Observability Stack**: OpenTelemetry + Serilog for comprehensive monitoring
-   **Messaging Infrastructure**: WolverineFx with PostgreSQL event sourcing
-   **Database Access**: PostgreSQL configuration with Npgsql
-   **Resilience Patterns**: HTTP resilience policies and circuit breakers
-   **Service Discovery**: Built-in service resolution and health checks
-   **Validation**: FluentValidation integration and dependency injection

## Getting Started

### Prerequisites

-   .NET 9.0 or later
-   ASP.NET Core 9.0 or later (includes `Microsoft.AspNetCore.App` framework reference)

### Basic Setup

#### Minimal API Example

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add service defaults - configures all common services
builder.AddServiceDefaults();

// Add your application-specific services
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Map default endpoints (health checks, metrics, etc.)
app.MapDefaultEndpoints();

// Add your application routes
app.MapGet("/api/users", (IUserService userService) =>
    userService.GetUsersAsync());

app.Run();
```

#### Worker Service Example

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);

// Add service defaults for background services
builder.AddServiceDefaults();

// Add your worker services
builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
await host.RunAsync();
```

## Configuration

### What Gets Configured

When you call `AddServiceDefaults()`, the following services are automatically configured:

#### 1. Health Checks

-   **Endpoints**: `/status` (liveness), `/health/internal` (readiness), `/health` (public)
-   **Built-in Checks**: Database connectivity, messaging health
-   **Custom Checks**: Extensible health check system

```csharp
// Automatically available endpoints:
// GET /status            - Liveness probe (cached, fast)
// GET /health/internal   - Readiness probe (localhost only)
// GET /health            - Public health (requires auth, detailed)
```

#### 2. OpenTelemetry Observability

-   **Metrics**: Application and infrastructure metrics
-   **Tracing**: Distributed request tracing
-   **Logging**: Structured logging with correlation

```csharp
// Configuration includes:
// - ASP.NET Core instrumentation
// - HTTP client instrumentation
// - Runtime metrics
// - GrPC client instrumentation
// - OTLP exporter for telemetry data
```

#### 3. Serilog Structured Logging

-   **Integration**: OpenTelemetry correlation
-   **Enrichment**: Request context and exceptions
-   **Sinks**: Console and OpenTelemetry sinks

```csharp
// Logging configuration:
// - Structured JSON output
// - Exception details with Serilog.Exceptions
// - Correlation IDs and trace context
```

#### 4. Service Discovery

-   **Resolution**: Automatic service endpoint resolution
-   **Load Balancing**: Built-in load balancing strategies
-   **Health-aware**: Integrates with health check system

#### 5. HTTP Resilience

-   **Policies**: Retry, circuit breaker, timeout patterns
-   **Configuration**: Adaptive resilience strategies
-   **Observability**: Resilience metrics and logging

### Connection Strings

Configure your services using standard connection string patterns:

```json
// appsettings.json
{
    "ConnectionStrings": {
        "Database": "Host=localhost;Database=myapp;Username=postgres;Password=password",
        "Messaging": "localhost:9092"
    }
}
```

### OpenTelemetry Configuration

Control observability settings:

```json
// appsettings.json
{
    "OpenTelemetry": {
        "ServiceName": "MyApp",
        "ServiceVersion": "1.0.0",
        "Endpoint": "http://localhost:4317"
    },
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning",
            "System": "Warning"
        }
    }
}
```

## Advanced Configuration

### Custom Health Checks

Add application-specific health checks:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add custom health checks
builder.Services.AddHealthChecks()
    .AddCheck<ExternalApiHealthCheck>("external-api")
    .AddCheck<CacheHealthCheck>("redis-cache");
```

### Custom Telemetry

Extend OpenTelemetry configuration:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add custom telemetry
builder.Services.Configure<OpenTelemetryOptions>(options =>
{
    options.AddSource("MyApp.CustomActivities");
});
```

### WolverineFx Message Handling

Configure messaging with WolverineFx:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// WolverineFx is automatically configured with PostgreSQL persistence
// Add your message handlers
builder.Services.AddScoped<OrderCreatedHandler>();
builder.Services.AddScoped<PaymentProcessedHandler>();

var app = builder.Build();
app.MapDefaultEndpoints();
app.Run();
```

### Custom Resilience Policies

Customize HTTP resilience:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Customize resilience for specific HTTP clients
builder.Services.AddHttpClient<ExternalApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.external.com");
})
.AddResilienceHandler("external-api", static builder =>
{
    builder.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential
    });
});
```

## Environment-Specific Configuration

### Development

```json
// appsettings.Development.json
{
    "ConnectionStrings": {
        "Database": "Host=localhost;Database=myapp_dev;Username=postgres;Password=dev123"
    },
    "OpenTelemetry": {
        "Endpoint": "http://localhost:4317"
    },
    "Logging": {
        "LogLevel": {
            "Default": "Debug"
        }
    }
}
```

### Production

```json
// appsettings.Production.json
{
    "ConnectionStrings": {
        "Database": "Host=prod-db;Database=myapp;Username=app_user;Password=${DB_PASSWORD}"
    },
    "OpenTelemetry": {
        "Endpoint": "https://telemetry.company.com"
    },
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning"
        }
    }
}
```

## Integrated Dependencies

This package includes comprehensive integrations with:

| Package                                            | Purpose                              |
| -------------------------------------------------- | ------------------------------------ |
| **Aspire.Npgsql**                                  | PostgreSQL database connectivity     |
| **CloudNative.CloudEvents.SystemTextJson**         | CloudEvents specification support    |
| **FluentValidation.DependencyInjectionExtensions** | Validation framework integration     |
| **Microsoft.Extensions.Http.Resilience**           | HTTP client resilience patterns      |
| **Microsoft.Extensions.ServiceDiscovery**          | Service discovery capabilities       |
| **OpenTelemetry Suite**                            | Complete observability stack         |
| **Serilog.AspNetCore**                             | Structured logging with ASP.NET Core |
| **Serilog.Exceptions**                             | Enhanced exception logging           |
| **Serilog.Sinks.OpenTelemetry**                    | OpenTelemetry sink for Serilog       |
| **WolverineFx**                                    | Messaging infrastructure             |
| **WolverineFx.Postgresql**                         | PostgreSQL integration for messaging |

## Target Frameworks

-   **.NET 9.0**: Primary target framework
-   **ASP.NET Core 9.0**: Includes framework reference for web applications
-   Compatible with all .NET Aspire host types

## Use Cases

This package is essential for:

-   **API Services**: REST and gRPC API projects
-   **Background Services**: Worker services and hosted services
-   **Orleans Silos**: Actor-based stateful services
-   **Message Processors**: Event-driven microservices
-   **Any Aspire Host**: All .NET Aspire-based applications

## Related Packages

-   [Momentum.ServiceDefaults.Api](../Momentum.ServiceDefaults.Api/README.md) - API-specific extensions
-   [Momentum.Extensions.Messaging.Kafka](../Momentum.Extensions.Messaging.Kafka/README.md) - Kafka messaging
-   [Momentum.Extensions](../Momentum.Extensions/README.md) - Core utilities and abstractions

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/vgmello/momentum/blob/main/LICENSE) file for details.

## Contributing

For contribution guidelines and more information about the Momentum platform, visit the [main repository](https://github.com/vgmello/momentum).
