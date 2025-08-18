# Service Defaults in Momentum

Service defaults provide a comprehensive set of pre-configured services and middleware that form the foundation of every Momentum application. They ensure consistency, observability, and best practices across all services.

## Overview

The `ServiceDefaultsExtensions` class provides a single entry point to configure all essential services:

```csharp
using Momentum.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add all Momentum service defaults
builder.AddServiceDefaults();

var app = builder.Build();

await app.RunAsync(args);
```

## What's Included

When you call `AddServiceDefaults()`, Momentum automatically configures:

1. **HTTPS Configuration** - Kestrel HTTPS setup
2. **Structured Logging** - Serilog with structured logging
3. **OpenTelemetry** - Distributed tracing, metrics, and logging
4. **Wolverine Messaging** - CQRS command/query bus and event publishing
5. **FluentValidation** - Automatic validator registration
6. **Health Checks** - Basic health check endpoints
7. **Service Discovery** - Service discovery configuration
8. **HTTP Client Resilience** - Retry policies and circuit breakers

## Core Configuration

### Basic Setup

```csharp
// Program.cs
using Momentum.ServiceDefaults;
using YourApp.Domain;

[assembly: DomainAssembly(typeof(IYourDomainAssembly))]

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

await app.RunAsync(args);
```

### Domain Assembly Marking

Mark your domain assemblies so Momentum can discover commands, queries, and validators:

```csharp
// In your domain assembly - IYourDomainAssembly.cs
namespace YourApp.Domain;

public interface IYourDomainAssembly;
```

```csharp
// In your API assembly - Program.cs
using YourApp.Domain;

[assembly: DomainAssembly(typeof(IYourDomainAssembly))]
```

**Why Domain Assemblies Matter:**
- Automatic discovery of command and query handlers
- FluentValidation validator registration
- Integration event discovery
- Wolverine message handler registration

## API Service Defaults

For API projects, add additional API-specific defaults:

```csharp
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;

var builder = WebApplication.CreateSlimBuilder(args);

// Core service defaults
builder.AddServiceDefaults();

// API-specific defaults
builder.AddApiServiceDefaults();

var app = builder.Build();

// Configure API middleware and endpoints
app.ConfigureApiUsingDefaults(requireAuth: false);

await app.RunAsync(args);
```

### API Configuration Options

```csharp
// With authentication required
app.ConfigureApiUsingDefaults(requireAuth: true);

// Custom configuration
app.ConfigureApiUsingDefaults(options =>
{
    options.RequireAuth = true;
    options.EnableSwagger = true;
    options.EnableCors = true;
    options.CorsPolicy = "MyPolicy";
});
```

## Service Discovery

Services are automatically registered for discovery:

```csharp
// Automatic service registration
builder.AddServiceDefaults();

// Services are discovered via:
// - Assembly scanning for DomainAssembly attributes
// - Service discovery configuration
// - Health check registrations
```

### Custom Service Registration

Add your own services alongside the defaults:

```csharp
builder.AddServiceDefaults();

// Add custom services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Add database contexts
builder.Services.AddDbContext<AppDb>(options =>
    options.UseNpgsql(connectionString));
```

## HTTP Client Configuration

All HTTP clients get automatic resilience policies:

```csharp
// Standard resilience is added automatically
builder.Services.ConfigureHttpClientDefaults(http =>
{
    // These are configured automatically by service defaults:
    http.AddStandardResilienceHandler();
});

// Custom HTTP client with additional configuration
builder.Services.AddHttpClient<IExternalService, ExternalService>(client =>
{
    client.BaseAddress = new Uri("https://api.external.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler(); // Already added by defaults
```

## Validation System

FluentValidation is automatically configured to discover validators:

```csharp
// Validators are automatically registered from:
// 1. Entry assembly
// 2. All assemblies marked with [DomainAssembly]

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).EmailAddress();
    }
}

// No manual registration needed - automatically discovered
```

### Manual Validator Registration

If needed, you can manually register validators:

```csharp
builder.AddServiceDefaults();

// Manual registration (usually not needed)
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
```

## Health Checks

Basic health checks are included by default:

```csharp
// Default health checks are registered automatically
builder.AddServiceDefaults();

// Map health check endpoints
app.MapDefaultHealthCheckEndpoints();

// Add custom health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddKafka(kafkaOptions => 
    {
        kafkaOptions.BootstrapServers = "localhost:9092";
    });
```

### Health Check Endpoints

The default endpoints are:

- `/health` - Overall health status
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

## Application Lifecycle

The `RunAsync()` method provides enhanced application lifecycle management:

```csharp
await app.RunAsync(args);
```

**Features:**
- Initialization logging
- Wolverine command-line support
- Proper exception handling
- Log flushing on shutdown

### Wolverine Commands

Supported command-line operations:
- `check-env` - Check environment configuration
- `codegen` - Generate code
- `db-apply` - Apply database migrations
- `db-assert` - Assert database state
- `db-dump` - Dump database schema
- `db-patch` - Create database patches
- `describe` - Describe configuration
- `help` - Show help
- `resources` - List resources
- `storage` - Storage operations

```bash
# Run application normally
dotnet run

# Run Wolverine command
dotnet run -- db-apply
dotnet run -- describe
```

## Configuration Examples

### Minimal API Service

```csharp
// Program.cs
using Momentum.ServiceDefaults;
using MyApp.Domain;

[assembly: DomainAssembly(typeof(IMyAppDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapGet("/health", () => "OK");

await app.RunAsync(args);
```

### Full API Service

```csharp
// Program.cs
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;
using MyApp.Domain;
using MyApp.Infrastructure;

[assembly: DomainAssembly(typeof(IMyAppDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);

// Core defaults
builder.AddServiceDefaults();
builder.AddApiServiceDefaults();

// Application-specific services
builder.AddMyAppServices();
builder.AddInfrastructure();

var app = builder.Build();

// Configure API
app.ConfigureApiUsingDefaults(requireAuth: true);
app.MapDefaultHealthCheckEndpoints();

// Add custom endpoints
app.MapControllers();
app.MapGet("/", () => "MyApp API");

await app.RunAsync(args);
```

### Background Service

```csharp
// Program.cs
using Momentum.ServiceDefaults;
using MyApp.Domain;

[assembly: DomainAssembly(typeof(IMyAppDomainAssembly))]

var builder = Host.CreateApplicationBuilder(args);

// Service defaults work with generic hosts too
builder.Services.AddServiceDefaults(builder);

// Add background services
builder.Services.AddHostedService<MyBackgroundService>();

var app = builder.Build();

await app.RunAsync();
```

## Environment Configuration

Service defaults adapt to different environments:

### Development
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "http://localhost:4317"
  }
}
```

### Production
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "MyApp": "Information"
    }
  },
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "https://otel.company.com"
  }
}
```

## Advanced Configuration

### Custom Entry Assembly

In some scenarios, you might need to specify a custom entry assembly:

```csharp
using Momentum.ServiceDefaults;

// Set custom entry assembly before calling AddServiceDefaults()
ServiceDefaultsExtensions.EntryAssembly = typeof(MyCustomMarker).Assembly;

builder.AddServiceDefaults();
```

### Selective Configuration

If you need more control, you can configure services individually:

```csharp
// Instead of AddServiceDefaults(), configure selectively:
builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddLogging();
builder.AddOpenTelemetry();
builder.AddWolverine();
builder.AddValidators();

builder.Services.AddHealthChecks();
builder.Services.AddServiceDiscovery();
```

## Integration with .NET Aspire

Service defaults work seamlessly with .NET Aspire:

```csharp
// AppHost project
var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.MyApp_Api>("myapp-api");

builder.Build().Run();
```

```csharp
// API project with Aspire integration
var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults(); // Includes Aspire service discovery
builder.AddApiServiceDefaults();

// Aspire automatically configures service discovery
var app = builder.Build();

await app.RunAsync(args);
```

## Troubleshooting

### Common Issues

**"Assembly not found" errors:**
```csharp
// Ensure you've marked domain assemblies
[assembly: DomainAssembly(typeof(IDomainMarker))]

// Or set manually
ServiceDefaultsExtensions.EntryAssembly = typeof(Program).Assembly;
```

**Validators not found:**
```csharp
// Check that validators are in domain assemblies
// Check that domain assemblies are marked with [DomainAssembly]

// Manual registration as fallback
builder.Services.AddValidatorsFromAssemblyContaining<MyValidator>();
```

**Service discovery not working:**
```csharp
// Ensure service defaults are added
builder.AddServiceDefaults();

// Check configuration
builder.Services.Configure<ServiceDiscoveryOptions>(options =>
{
    // Custom configuration
});
```

## Best Practices

### Assembly Organization
1. **Create domain marker interfaces**: Use interfaces to mark domain assemblies
2. **Mark all domain assemblies**: Every assembly with commands/queries needs `[DomainAssembly]`
3. **Use consistent naming**: Follow naming conventions for discoverable components

### Configuration Management
1. **Use appsettings.json**: Store configuration in standard .NET configuration files
2. **Environment-specific settings**: Use appsettings.Development.json, etc.
3. **Secure secrets**: Use Azure Key Vault, user secrets, or environment variables

### Service Registration
1. **Leverage automatic registration**: Let service defaults handle common services
2. **Register custom services after defaults**: Add your services after calling `AddServiceDefaults()`
3. **Use appropriate lifetimes**: Choose correct service lifetimes (Singleton, Scoped, Transient)

### Error Handling
1. **Use the RunAsync method**: Always use `app.RunAsync(args)` for proper lifecycle management
2. **Configure logging early**: Service defaults configure logging before other services
3. **Handle startup errors**: Use try/catch around configuration code if needed

## Next Steps

- Learn about [API Setup](./api-setup) for REST and gRPC configuration
- Understand [Observability](./observability) with OpenTelemetry
- Explore [CQRS](../cqrs/) patterns for commands and queries
- See [Messaging](../messaging/) for event-driven architecture