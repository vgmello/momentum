---
title: Service Configuration
description: Comprehensive service configuration and setup patterns for Momentum applications, including service defaults, .NET Aspire orchestration, observability, and deployment configuration.
date: 2024-01-15
---

# Service Configuration

Comprehensive service configuration and setup patterns for Momentum applications, including service defaults, .NET Aspire orchestration, observability, and multi-tenant deployment configuration.

## Overview

Momentum provides opinionated service configuration that follows .NET best practices while adding specialized functionality for microservices and domain-driven design:

- **Service Defaults**: Pre-configured common services and middleware with automatic discovery
- **Aspire Integration**: .NET Aspire orchestration for local development and service discovery
- **API Setup**: REST and gRPC endpoint configuration with OpenAPI documentation
- **Observability**: OpenTelemetry integration with structured logging, metrics, and distributed tracing
- **Port Allocation**: Systematic port management across development and production environments
- **Multi-Tenant Configuration**: Tenant-aware configuration patterns for SaaS applications
- **Environment Management**: Development, staging, and production configuration strategies

## Core Components

### Service Defaults
[Service Defaults](./service-defaults) provide pre-configured, production-ready service registration including Wolverine CQRS, FluentValidation, OpenTelemetry, and health checks with automatic assembly discovery.

### API Setup
[API Setup](./api-setup) configures REST and gRPC endpoints with OpenAPI documentation, automatic validation, exception handling, and security middleware.

### Observability
[Observability](./observability) implements comprehensive monitoring through OpenTelemetry integration, structured logging with Serilog, custom metrics, and distributed tracing.

### Port Allocation
[Port Allocation](./port-allocation) provides systematic port assignment patterns for .NET Aspire, Docker Compose, and production deployment environments.

## Configuration Patterns

### .NET Aspire Orchestration

Momentum templates include .NET Aspire orchestration for seamless local development:

```csharp
// AppHost project - orchestrates all services
var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database with automatic connection string management
var database = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("AppDomainDb");

// Kafka message broker
var kafka = builder.AddKafka("kafka")
    .WithKafkaUI();

// API service with service discovery
var api = builder.AddProject<Projects.AppDomain_Api>("api")
    .WithReference(database)
    .WithReference(kafka)
    .WithHttpEndpoint(port: 8101, name: "http")
    .WithHttpsEndpoint(port: 8111, name: "https");

// BackOffice service for background processing
var backOffice = builder.AddProject<Projects.AppDomain_BackOffice>("backoffice")
    .WithReference(database)
    .WithReference(kafka);

builder.Build().Run();
```

### Layered Configuration Hierarchy

Configuration follows the standard .NET configuration hierarchy with Momentum enhancements:

1. **appsettings.json**: Base application configuration
2. **appsettings.{Environment}.json**: Environment-specific overrides
3. **Environment Variables**: Container and cloud-friendly runtime configuration
4. **User Secrets**: Development-time secure storage
5. **Cloud Key Vault/Secret Manager**: Production secrets management
6. **Aspire Service Discovery**: Automatic service endpoint resolution

### Service Registration with Domain Assembly Discovery

```csharp
using Momentum.ServiceDefaults;
using AppDomain.Domain;

// Mark domain assemblies for automatic discovery
[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

var builder = WebApplication.CreateBuilder(args);

// Core Momentum service defaults with automatic discovery
builder.AddServiceDefaults();

// API-specific configuration (if building an API service)
builder.AddApiServiceDefaults();

// Custom application services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddDbContext<AppDomainDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDomainDb")));

var app = builder.Build();

// Configure middleware pipeline
app.ConfigureApiUsingDefaults(requireAuth: true);

// Map endpoints
app.MapControllers();
app.MapDefaultHealthCheckEndpoints();

await app.RunAsync(args);
```

### Configuration Validation

Implement configuration validation at startup:

```csharp
// Configuration model with validation
public class AppConfiguration
{
    [Required]
    public string ApplicationName { get; set; } = string.Empty;

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Range(1, 3600)]
    public int TokenExpirationMinutes { get; set; } = 60;
}

// Register and validate configuration
builder.Services.Configure<AppConfiguration>(
    builder.Configuration.GetSection("App"));

builder.Services.AddOptions<AppConfiguration>()
    .BindConfiguration("App")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## Key Features

### Automatic Service Discovery

Momentum automatically discovers and registers services from marked assemblies:

- **CQRS Handlers**: Command and query handlers with Wolverine
- **Validators**: FluentValidation validators for all commands and queries
- **Integration Events**: Event handlers for cross-service communication
- **Database Commands**: Source-generated database command handlers

### Multi-Tenant Configuration

Built-in support for multi-tenant scenarios:

```csharp
// Tenant-aware configuration
public class TenantConfiguration
{
    public string TenantId { get; set; } = string.Empty;
    public string DatabaseConnectionString { get; set; } = string.Empty;
    public Dictionary<string, string> Features { get; set; } = new();
}

// Register tenant configuration provider
builder.Services.AddSingleton<ITenantConfigurationProvider, TenantConfigurationProvider>();
```

### Production-Ready Defaults

- **OpenTelemetry Integration**: Distributed tracing, metrics, and logging
- **Health Checks**: Comprehensive application and infrastructure monitoring
- **Middleware Pipeline**: Pre-configured exception handling, CORS, and security
- **Configuration Validation**: Startup validation with clear error messages
- **Secrets Management**: Azure Key Vault and user secrets integration
- **Hot Reload**: Development-time configuration and code updates

## Service Categories

### Infrastructure Services
- Database connections
- Message broker configuration
- Caching setup
- External API clients

### Application Services
- CQRS handlers
- Domain services
- Integration services
- Background services

### Cross-Cutting Services
- Logging and telemetry
- Authentication and authorization
- Validation and error handling
- Request/response middleware

## Environment Management

### Development with .NET Aspire

Local development is orchestrated through .NET Aspire for optimal developer experience:

```bash
# Start the complete application stack
dotnet run --project src/AppDomain.AppHost

# Access Aspire Dashboard at https://localhost:18110
```

**Development Features:**
- **Automatic Service Discovery**: Services find each other automatically
- **Infrastructure Management**: PostgreSQL and Kafka containers managed by Aspire
- **Hot Reload**: Code and configuration changes applied instantly
- **Structured Logging**: All service logs aggregated in Aspire dashboard
- **Health Monitoring**: Real-time health status of all services

### Staging Environment

Production-like environment for validation and performance testing:

```json
// appsettings.Staging.json
{
  "ConnectionStrings": {
    "AppDomainDb": "Host=staging-db;Database=appdomain;Username=appuser"
  },
  "Kafka": {
    "BootstrapServers": "staging-kafka:9092"
  },
  "OpenTelemetry": {
    "Endpoint": "https://staging-otel.company.com"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Production Deployment

Optimized for performance, security, and reliability:

```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "AppDomainDb": "$(DB_CONNECTION_STRING)" // From Key Vault
  },
  "Kafka": {
    "BootstrapServers": "$(KAFKA_BOOTSTRAP_SERVERS)"
  },
  "OpenTelemetry": {
    "Endpoint": "$(OTEL_EXPORTER_OTLP_ENDPOINT)"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "AppDomain": "Information"
    }
  },
  "HealthChecks": {
    "Enabled": true,
    "TimeoutSeconds": 30
  }
}
```

**Production Considerations:**
- **Security Hardening**: Secrets stored in Azure Key Vault
- **Performance Optimization**: Connection pooling and caching enabled
- **Monitoring**: Comprehensive telemetry and alerting
- **High Availability**: Load balancing and failover configuration
- **Disaster Recovery**: Backup and restore procedures

## Configuration Sources

1. **Default Settings**: Built-in sensible defaults
2. **Configuration Files**: JSON, XML, INI file support
3. **Environment Variables**: Container and cloud-friendly
4. **Command Line**: Runtime parameter override
5. **External Providers**: Azure Key Vault, AWS Parameter Store

## Best Practices

- Use strongly-typed configuration classes
- Validate configuration at startup
- Separate environment-specific settings
- Use secrets management for sensitive data
- Document configuration options
- Provide sensible defaults

## Common Configuration Scenarios

### API Service Configuration

Complete setup for a REST/gRPC API service:

```csharp
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;
using AppDomain.Domain;

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);

// Core Momentum configuration
builder.AddServiceDefaults();
builder.AddApiServiceDefaults();

// Database context with connection string from Aspire
builder.Services.AddDbContext<AppDomainDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDomainDb")));

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
    });

var app = builder.Build();

// Configure middleware pipeline with authentication
app.ConfigureApiUsingDefaults(requireAuth: true);

// Map endpoints
app.MapControllers();
app.MapDefaultHealthCheckEndpoints();

await app.RunAsync(args);
```

### Background Service Configuration

Setup for asynchronous message processing services:

```csharp
using Momentum.ServiceDefaults;
using AppDomain.Domain;

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

var builder = Host.CreateApplicationBuilder(args);

// Service defaults work with generic hosts
builder.Services.AddServiceDefaults(builder);

// Database and messaging
builder.Services.AddDbContext<AppDomainDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDomainDb")));

// Background services
builder.Services.AddHostedService<EventProcessingService>();

var app = builder.Build();

await app.RunAsync();
```

### Orleans Stateful Service Configuration

Setup for Orleans-based stateful processing:

```csharp
using Momentum.ServiceDefaults;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddServiceDefaults(builder);

// Orleans configuration
builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering()
        .ConfigureLogging(logging => logging.AddConsole())
        .UseDashboard(options => { });
});

// Register grain services
builder.Services.AddScoped<IGrainService, GrainService>();

var app = builder.Build();

await app.RunAsync();
```

### Multi-Tenant Service Configuration

Configuration for SaaS applications with tenant isolation:

```csharp
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
builder.AddApiServiceDefaults();

// Multi-tenant database context
builder.Services.AddDbContext<AppDomainDb>((serviceProvider, options) =>
{
    var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();
    var connectionString = tenantProvider.GetConnectionString();
    options.UseNpgsql(connectionString);
});

// Tenant resolution middleware
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<TenantResolutionMiddleware>();

var app = builder.Build();

// Add tenant resolution before other middleware
app.UseMiddleware<TenantResolutionMiddleware>();
app.ConfigureApiUsingDefaults();

await app.RunAsync(args);
```

## Getting Started

### 1. Install the Momentum Template

```bash
# Install the template (if working from source)
dotnet new install .

# Create a new project
dotnet new mmt -n MyBusinessApp --aspire --api --back-office
```

### 2. Configure Service Defaults

Mark your domain assemblies and add service defaults:

```csharp
// In your domain assembly
namespace MyBusinessApp.Domain;
public interface IMyBusinessAppDomainAssembly;

// In Program.cs
using MyBusinessApp.Domain;
[assembly: DomainAssembly(typeof(IMyBusinessAppDomainAssembly))]

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
```

### 3. Start with Aspire Orchestration

```bash
# Start the complete application stack
dotnet run --project src/MyBusinessApp.AppHost

# Open Aspire Dashboard: https://localhost:18110
```

### 4. Add Environment-Specific Configuration

Create configuration files for each environment:

- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

### 5. Configure Production Deployment

Set up Azure Key Vault or equivalent secrets management:

```csharp
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddAzureKeyVault(
        vaultUri: new Uri(builder.Configuration["KeyVault:VaultUri"]!),
        credential: new DefaultAzureCredential());
}
```

### 6. Monitor and Observe

Momentum includes comprehensive observability out-of-the-box:

- **Structured Logging**: Serilog with OpenTelemetry integration
- **Distributed Tracing**: Automatic tracing across services
- **Health Checks**: Application and infrastructure monitoring
- **Metrics**: Business and technical metrics collection

## Troubleshooting Common Issues

### Service Discovery Problems

**Issue**: Services can't find each other in Aspire environment

**Solution**:
```csharp
// Ensure service names match between AppHost and service configuration
var api = builder.AddProject<Projects.AppDomain_Api>("api"); // Name must match

// In consuming service, use the exact name:
var httpClient = serviceProvider.GetRequiredService<HttpClient>();
var response = await httpClient.GetAsync("https://api/health");
```

### Configuration Validation Errors

**Issue**: Application fails to start due to configuration validation

**Solution**:
```csharp
// Add detailed configuration validation
builder.Services.AddOptions<MyConfiguration>()
    .BindConfiguration("MySection")
    .ValidateDataAnnotations()
    .Validate(config => !string.IsNullOrEmpty(config.RequiredProperty),
        "RequiredProperty cannot be empty")
    .ValidateOnStart();
```

### Assembly Discovery Issues

**Issue**: Commands, queries, or validators not being discovered

**Solution**:
```csharp
// Ensure all domain assemblies are marked
[assembly: DomainAssembly(typeof(IDomainMarker))]

// Or manually specify assemblies
ServiceDefaultsExtensions.EntryAssembly = typeof(Program).Assembly;
```

### Health Check Failures

**Issue**: Health checks failing during startup

**Solution**:
```csharp
// Add health check timeout and retry policies
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, timeout: TimeSpan.FromSeconds(30))
    .AddCheck("startup-delay", () =>
    {
        // Allow time for dependencies to start
        return Task.FromResult(HealthCheckResult.Healthy());
    });
```

### OpenTelemetry Not Working

**Issue**: Telemetry data not appearing in monitoring tools

**Solution**:
```json
// Verify OpenTelemetry configuration
{
  "OpenTelemetry": {
    "Endpoint": "http://localhost:4317",
    "ServiceName": "MyService",
    "ServiceVersion": "1.0.0"
  }
}
```

## Best Practices Summary

1. **Use .NET Aspire for Local Development**: Leverage Aspire's orchestration capabilities for the best developer experience
2. **Mark Domain Assemblies**: Always mark assemblies containing domain logic with `[DomainAssembly]`
3. **Validate Configuration Early**: Use configuration validation to catch issues at startup
4. **Implement Comprehensive Health Checks**: Monitor all critical dependencies
5. **Use Structured Logging**: Include relevant context in all log messages
6. **Secure Production Secrets**: Never store secrets in configuration files
7. **Monitor Everything**: Implement observability from day one
8. **Test Configuration**: Include configuration testing in your test suites

## Related Topics

- **[Service Defaults](./service-defaults.md)**: Detailed service defaults configuration and automatic discovery
- **[API Setup](./api-setup.md)**: REST and gRPC API configuration patterns
- **[Observability](./observability.md)**: OpenTelemetry integration and monitoring setup
- **[Port Allocation](./port-allocation.md)**: Systematic port management across environments
- **[CQRS](../cqrs/index.md)**: Command and query patterns with Wolverine
- **[Messaging](../messaging/index.md)**: Event-driven architecture with Kafka
- **[Database](../database/index.md)**: Database setup and migration patterns
- **[Testing](../testing/index.md)**: Integration and unit testing strategies
