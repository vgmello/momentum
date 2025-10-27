# Momentum.ServiceDefaults.Api

API-specific service defaults for Momentum platform extending the base ServiceDefaults with gRPC, OpenAPI documentation (Scalar), and API-focused configurations. Essential for Momentum API projects.

## Overview

The `Momentum.ServiceDefaults.Api` package extends `Momentum.ServiceDefaults` with additional configurations specifically designed for API projects. It provides comprehensive support for both REST and gRPC APIs, along with modern documentation tools.

## Installation

Add the package to your project using the .NET CLI:

```bash
dotnet add package Momentum.ServiceDefaults.Api
```

Or using the Package Manager Console:

```powershell
Install-Package Momentum.ServiceDefaults.Api
```

## Key Features

-   **All ServiceDefaults Features**: Inherits complete observability, messaging, and database capabilities
-   **gRPC Support**: Full gRPC server configuration with reflection and web support
-   **OpenAPI Integration**: Automatic OpenAPI 3.0 specification generation
-   **Scalar Documentation**: Modern, interactive API documentation interface
-   **Enhanced Telemetry**: API-specific metrics, tracing, and monitoring

## Getting Started

### Prerequisites

-   .NET 9.0 or later
-   ASP.NET Core 9.0 or later
-   All prerequisites from Momentum.ServiceDefaults

### Basic Usage

#### REST API Example

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add API service defaults - includes all base features plus API-specific configurations
builder.AddApiServiceDefaults();

// Add your API services
builder.Services.AddControllers();

var app = builder.Build();

// Map default endpoints (health, metrics, documentation)
app.MapDefaultEndpoints();

// Map your API endpoints
app.MapControllers();

app.Run();
```

#### gRPC Service Example

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddApiServiceDefaults();

// Add gRPC services (automatically configured)
builder.Services.AddGrpc();

var app = builder.Build();

app.MapDefaultEndpoints();

// Map gRPC services
app.MapGrpcService<UserService>();
app.MapGrpcService<OrderService>();

app.Run();
```

#### Hybrid API (REST + gRPC)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddApiServiceDefaults();

// Add both REST and gRPC support
builder.Services.AddControllers();
builder.Services.AddGrpc();

var app = builder.Build();

app.MapDefaultEndpoints();

// Map both REST and gRPC endpoints
app.MapControllers();
app.MapGrpcService<UserService>();

app.Run();
```

## What Gets Configured

In addition to all `Momentum.ServiceDefaults` features, this package automatically configures:

### 1. gRPC Services

-   **gRPC Server**: Full ASP.NET Core gRPC support
-   **Server Reflection**: gRPC reflection for tool support
-   **gRPC-Web**: Browser-compatible gRPC over HTTP

### 2. OpenAPI Documentation

-   **Specification Generation**: Automatic OpenAPI 3.0 spec generation
-   **Endpoint Discovery**: Automatic API endpoint documentation
-   **Schema Generation**: Request/response model documentation

### 3. Scalar API Documentation

-   **Interactive UI**: Modern API documentation interface
-   **Try It Out**: Built-in API testing capabilities
-   **Automatic Discovery**: Works with your OpenAPI specification

### 4. Enhanced Observability

-   **gRPC Telemetry**: Metrics and tracing for gRPC calls
-   **API Metrics**: Request rates, latencies, and error rates
-   **Documentation Metrics**: API documentation usage tracking

## Available Endpoints

When you call `app.MapDefaultEndpoints()`, you get:

| Endpoint     | Purpose                            |
| ------------ | ---------------------------------- |
| `/status`    | Liveness probe (cached, no auth)   |
| `/health/internal` | Readiness probe (localhost only) |
| `/health`    | Public health (requires auth)      |
| `/scalar/v1` | Interactive API documentation      |

## Configuration

### OpenAPI Settings

```json
// appsettings.json
{
    "OpenApi": {
        "Document": {
            "Title": "My API",
            "Version": "v1",
            "Description": "API for my application"
        }
    }
}
```

### gRPC Configuration

```json
// appsettings.json
{
    "Grpc": {
        "EnableDetailedErrors": true,
        "MaxReceiveMessageSize": 4194304,
        "MaxSendMessageSize": 4194304
    }
}
```

## Advanced Usage

### Custom OpenAPI Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddApiServiceDefaults();

// Customize OpenAPI generation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Contact = new() { Name = "Support", Email = "support@company.com" };
        return Task.CompletedTask;
    });
});
```

### Custom gRPC Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddApiServiceDefaults();

// Customize gRPC services
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 8 * 1024 * 1024; // 8MB
});
```

## Integrated Dependencies

This package includes:

| Package                               | Purpose                              |
| ------------------------------------- | ------------------------------------ |
| **Grpc.AspNetCore**                   | gRPC server framework                |
| **Grpc.AspNetCore.Server.Reflection** | gRPC reflection support              |
| **Grpc.AspNetCore.Web**               | gRPC-Web support for browsers        |
| **Microsoft.AspNetCore.OpenApi**      | OpenAPI 3.0 specification generation |
| **Scalar.AspNetCore**                 | Modern API documentation UI          |
| **OpenTelemetry.Extensions.Hosting**  | Enhanced telemetry                   |
| **Momentum.Extensions.Abstractions**  | Core abstractions                    |
| **Momentum.Extensions.XmlDocs**       | XML documentation processing         |

## Target Frameworks

-   **.NET 9.0**: Primary target framework
-   **ASP.NET Core 9.0**: Includes framework reference

## Use Cases

This package is ideal for:

-   **REST APIs**: Traditional HTTP APIs with OpenAPI documentation
-   **gRPC Services**: High-performance RPC services
-   **Hybrid APIs**: Services supporting both REST and gRPC protocols
-   **API Gateways**: Services that aggregate other APIs
-   **Microservice APIs**: Services in event-driven architectures

## Related Packages

-   [Momentum.ServiceDefaults](../Momentum.ServiceDefaults/README.md) - Base service configuration
-   [Momentum.Extensions.Messaging.Kafka](../Momentum.Extensions.Messaging.Kafka/README.md) - Kafka messaging
-   [Momentum.Extensions.Abstractions](../Momentum.Extensions.Abstractions/README.md) - Core abstractions

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/vgmello/momentum/blob/main/LICENSE) file for details.

## Contributing

For contribution guidelines and more information about the Momentum platform, visit the [main repository](https://github.com/vgmello/momentum).
