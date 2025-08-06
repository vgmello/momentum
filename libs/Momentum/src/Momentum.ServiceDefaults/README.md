# Momentum.ServiceDefaults

Service defaults for the Momentum platform providing common configurations for Aspire-based services. This package contains essential configurations that should be used by all Momentum host projects (APIs, background services, Orleans silos, etc.).

## Installation

```bash
dotnet add package Momentum.ServiceDefaults
```

## Features

-   **Aspire Integration**: Full .NET Aspire service defaults implementation
-   **Messaging Infrastructure**: Kafka integration with CloudEvents support via WolverineFx
-   **Database Access**: PostgreSQL configuration with Npgsql
-   **Observability**: OpenTelemetry metrics, traces, and logs with Serilog
-   **Resilience**: HTTP resilience patterns and policies
-   **Service Discovery**: Built-in service discovery capabilities
-   **Health Checks**: Comprehensive health check infrastructure

## Dependencies

This package includes configurations for:

-   Aspire.Confluent.Kafka - Kafka messaging
-   Aspire.Npgsql - PostgreSQL database access
-   CloudNative.CloudEvents - CloudEvents support
-   FluentValidation - Validation infrastructure
-   Microsoft.Extensions.Http.Resilience - HTTP resilience
-   Microsoft.Extensions.ServiceDiscovery - Service discovery
-   OpenTelemetry suite - Observability
-   Serilog - Structured logging
-   WolverineFx - Message bus and handlers

## Basic Usage

### In Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add service defaults - this configures all the common services
builder.AddServiceDefaults();

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

app.Run();
```

### What Gets Configured

When you call `AddServiceDefaults()`, the following is automatically configured:

1. **Health Checks** - `/health` and `/alive` endpoints
2. **OpenTelemetry** - Metrics, traces, and logs export
3. **Serilog** - Structured logging with OpenTelemetry integration
4. **Service Discovery** - Automatic service resolution
5. **Resilience** - HTTP client resilience policies
6. **Environment** - Proper configuration loading

## Important Notes

This package is designed to be used by all Momentum host projects to ensure consistency across the platform. It includes a `FrameworkReference` to `Microsoft.AspNetCore.App`, making it suitable for ASP.NET Core applications.

## Requirements

-   .NET 9.0 or later
-   ASP.NET Core 9.0 or later

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/vgmello/momentum-sample/blob/main/LICENSE) file for details.

## Contributing

For more information about the Momentum platform and contribution guidelines, please visit the [main repository](https://github.com/vgmello/momentum-sample).
