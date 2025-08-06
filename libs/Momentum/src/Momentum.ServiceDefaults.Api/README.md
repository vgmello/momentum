# Momentum.ServiceDefaults.Api

API-specific service defaults for the Momentum platform that extend the base ServiceDefaults with additional configurations specifically needed for API projects. This package includes gRPC support, OpenAPI documentation, and API-focused service configurations.

## Installation

```bash
dotnet add package Momentum.ServiceDefaults.Api
```

## Features

-   **All ServiceDefaults Features**: Inherits all capabilities from Momentum.ServiceDefaults
-   **gRPC Support**: Full gRPC server configuration with reflection and web support
-   **OpenAPI Integration**: Microsoft.AspNetCore.OpenApi for API documentation
-   **Scalar UI**: Modern, interactive API documentation interface
-   **API Telemetry**: Extended telemetry specifically for API scenarios

## Dependencies

This package includes:

-   **gRPC Stack**: Grpc.AspNetCore, Grpc.AspNetCore.Server.Reflection, Grpc.AspNetCore.Web
-   **OpenAPI**: Microsoft.AspNetCore.OpenApi for OpenAPI 3.0 specification generation
-   **Documentation**: Scalar.AspNetCore for interactive API documentation
-   **Observability**: OpenTelemetry.Extensions.Hosting for enhanced telemetry
-   **Abstractions**: Momentum.Extensions.Abstractions for core types

## Basic Usage

### In Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add API service defaults - includes all base service defaults plus API-specific services
builder.AddApiServiceDefaults();

// Add your API services
builder.Services.AddControllers();
builder.Services.AddGrpc();

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Map your API endpoints
app.MapControllers();
app.MapGrpcServices();

app.Run();
```

### What Gets Configured

In addition to all base ServiceDefaults configurations, this package adds:

1. **gRPC Services** - Server, reflection, and web support
2. **OpenAPI** - Automatic OpenAPI specification generation
3. **Scalar Documentation** - Interactive API documentation UI
4. **Enhanced Telemetry** - API-specific metrics and tracing

## Use Cases

This package is ideal for:

-   REST APIs
-   gRPC services
-   Hybrid APIs (REST + gRPC)
-   API gateways
-   Any service that exposes HTTP endpoints

## Requirements

-   .NET 9.0 or later
-   ASP.NET Core 9.0 or later

## Architecture

This package extends Momentum.ServiceDefaults, so you get all the base functionality (messaging, database, observability) plus API-specific additions. It's designed to be the standard foundation for all Momentum API projects.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/vgmello/momentum-sample/blob/main/LICENSE) file for details.

## Contributing

For more information about the Momentum platform and contribution guidelines, please visit the [main repository](https://github.com/vgmello/momentum-sample).
