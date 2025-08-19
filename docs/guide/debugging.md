---
title: Debugging Tips
description: Essential debugging techniques and tools for AppDomain Solution development
date: 2025-01-07
---

# Debugging Tips

This guide provides practical debugging techniques and tools to help you diagnose and resolve issues in the AppDomain Solution effectively.

## Prerequisites

-   [Development environment setup](/guide/dev-setup)
-   Basic understanding of .NET debugging concepts
-   Familiarity with the AppDomain Solution architecture

## Local Development Debugging

### Using .NET Aspire Dashboard

The .NET Aspire AppHost provides a comprehensive dashboard for monitoring your local development environment.

**Starting the Dashboard**:

```bash
dotnet run --project src/AppDomain.AppHost
```

**Dashboard Features**:

-   **Services Overview**: Monitor health and status of all services
-   **Logs Aggregation**: View consolidated logs from all services
-   **Metrics**: Track performance counters and custom metrics
-   **Distributed Tracing**: Follow requests across service boundaries
-   **Resource Management**: Monitor database connections and external dependencies

[!TIP]
Access the dashboard at `http://localhost:15888` when running the AppHost locally.

### Service-Level Debugging

**API Service Debugging**:

```bash
# Debug the API service directly
dotnet run --project src/AppDomain.Api --launch-profile https
```

**BackOffice Service Debugging**:

```bash
# Debug background processing
dotnet run --project src/AppDomain.BackOffice
```

**Orleans Silo Debugging**:

```bash
# Debug stateful Orleans processing
dotnet run --project src/AppDomain.BackOffice.Orleans
```

### Database Connection Issues

**Common PostgreSQL Connection Problems**:

```bash
# Check if database is running
docker compose ps AppDomain-db

# View database logs
docker compose logs AppDomain-db

# Reset database completely
docker compose down -v
docker compose up AppDomain-db AppDomain-db-migrations
```

**Connection String Verification**:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Port=54320;Database=AppDomain;Username=postgres;Password=password;"
    }
}
```

## Application-Level Debugging

### CQRS Command/Query Debugging

**Debugging Command Handlers**:

```csharp
[DbCommand(sp: "app_domain.cashiers_create")]
public record CreateCashierCommand : IRequest<Result<Cashier>>
{
    // Set breakpoints in generated handlers
    // Check parameter mapping to stored procedures
}
```

**Common Issues**:

-   **Missing Parameters**: Verify stored procedure parameter names match command properties
-   **Type Mismatches**: Ensure parameter types align with database expectations
-   **Null Values**: Check for required properties and nullable database columns

### Event-Driven Architecture Debugging

**Message Bus Issues**:

```csharp
// Enable detailed Wolverine logging
services.AddWolverine(opts =>
{
    opts.LocalQueue("local")
        .UseDurableInbox(); // Check for message persistence issues
});
```

**Event Publishing Problems**:

1. **Event Not Triggered**: Check if integration event is properly published
2. **Handler Not Executed**: Verify message routing configuration
3. **Serialization Issues**: Ensure event properties are serializable

**Debugging Event Handlers**:

```csharp
public class CashierCreatedHandler
{
    public async Task Handle(CashierCreated @event)
    {
        // Add logging to track event processing
        _logger.LogInformation("Processing CashierCreated event for {CashierId}", @event.CashierId);

        // Set breakpoints here to inspect event data
    }
}
```

### Orleans Grain Debugging

**Grain Activation Issues**:

```csharp
public class InvoiceGrain : Grain, IInvoiceGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Debug grain lifecycle
        _logger.LogInformation("Invoice grain {GrainId} activating", this.GetPrimaryKey());
        await base.OnActivateAsync(cancellationToken);
    }
}
```

**State Persistence Problems**:

1. **State Not Saved**: Check grain state configuration
2. **State Corruption**: Verify serialization/deserialization
3. **Concurrent Access**: Review grain reentrancy settings

## Logging and Observability

### Structured Logging

**Configure Serilog for Development**:

```json
{
    "Serilog": {
        "Using": ["Serilog.Sinks.Console"],
        "MinimumLevel": {
            "Default": "Information",
            "Override": {
                "Microsoft": "Warning",
                "Microsoft.Hosting.Lifetime": "Information",
                "AppDomain": "Debug"
            }
        },
        "WriteTo": [
            {
                "Name": "Console",
                "Args": {
                    "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
                }
            }
        ]
    }
}
```

**Adding Correlation IDs**:

```csharp
// Track requests across service boundaries
public class CorrelationMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                          ?? Guid.NewGuid().ToString();

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Response.Headers.Add("X-Correlation-ID", correlationId);
            await next(context);
        }
    }
}
```

### Distributed Tracing

**OpenTelemetry Configuration**:

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddNpgsqlInstrumentation()
            .AddConsoleExporter(); // Development only
    });
```

**Custom Activity Sources**:

```csharp
private static readonly ActivitySource ActivitySource = new("AppDomain.Cashiers");

public async Task<Result<Cashier>> Handle(CreateCashierCommand request)
{
    using var activity = ActivitySource.StartActivity("CreateCashier");
    activity?.SetTag("cashier.name", request.Name);

    // Your handler logic here
}
```

## Performance Debugging

### Database Query Performance

**Enable Query Logging**:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Port=54320;Database=AppDomain;Username=postgres;Password=password;Log Parameters=true;"
    },
    "Logging": {
        "LogLevel": {
            "Npgsql.EntityFrameworkCore.PostgreSQL": "Information"
        }
    }
}
```

**Analyze Slow Queries**:

```sql
-- Enable slow query logging in PostgreSQL
ALTER SYSTEM SET log_min_duration_statement = 100; -- Log queries taking > 100ms
SELECT pg_reload_conf();

-- Monitor active queries
SELECT pid, query, state, query_start
FROM pg_stat_activity
WHERE state = 'active';
```

### Memory Usage Analysis

**dotnet-counters for Live Metrics**:

```bash
# Install dotnet-counters
dotnet tool install -g dotnet-counters

# Monitor API service
dotnet-counters monitor -n AppDomain.Api --refresh-interval 1

# Key metrics to watch:
# - GC heap size
# - Allocation rate
# - Working set
# - Request rate
```

**Memory Dumps for Analysis**:

```bash
# Capture memory dump
dotnet-dump collect -p <process-id>

# Analyze with dotnet-dump
dotnet-dump analyze <dump-file>
```

## Integration Testing Debugging

### TestContainers Issues

**Common Container Problems**:

```csharp
[Test]
public async Task Should_Create_Cashier_Successfully()
{
    // Debug container startup issues
    var container = new PostgreSqlBuilder()
        .WithImage("postgres:15")
        .WithDatabase("AppDomain")
        .WithUsername("postgres")
        .WithPassword("password")
        .WithPortBinding(54320, 5432)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    await container.StartAsync();

    // Verify container health
    var connectionString = container.GetConnectionString();
    // ... test implementation
}
```

**TestContainers Troubleshooting**:

1. **Docker Issues**: Ensure Docker Desktop is running
2. **Port Conflicts**: Check for existing services on target ports
3. **Resource Limits**: Verify sufficient memory and CPU allocation
4. **Network Configuration**: Confirm container networking setup

### End-to-End Test Debugging

**API Integration Tests**:

```csharp
public class CashierApiTests : IClassFixture<AppDomainApiFactory>
{
    [Test]
    public async Task Should_Return_Created_Cashier()
    {
        // Enable detailed HTTP logging for debugging
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            });
        }).CreateClient();

        var request = new CreateCashierRequest("John Doe", "john@example.com");
        var response = await client.PostAsJsonAsync("/api/cashiers", request);

        // Debug response issues
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Error Response: {errorContent}");
        }

        response.EnsureSuccessStatusCode();
    }
}
```

## Troubleshooting Common Issues

### Startup Issues

**Service Registration Problems**:

```csharp
// Debug dependency injection issues
public void ConfigureServices(IServiceCollection services)
{
    // Log all registered services
    foreach (var service in services)
    {
        Console.WriteLine($"Service: {service.ServiceType.Name} -> {service.ImplementationType?.Name}");
    }
}
```

**Configuration Issues**:

```csharp
// Validate configuration during startup
public void Configure(IApplicationBuilder app, IConfiguration config)
{
    // Debug configuration values
    var connectionString = config.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Database connection string is not configured");
    }
}
```

### Event Processing Issues

**Message Queue Problems**:

1. **Messages Not Processing**: Check queue health and consumer status
2. **Dead Letter Queue**: Investigate failed message patterns
3. **Message Ordering**: Verify sequential processing requirements

**Integration Event Debugging**:

```csharp
// Add debugging to event handlers
public class AppDomainInboxHandler
{
    public async Task Handle(CashierCreated @event)
    {
        _logger.LogDebug("Received CashierCreated event: {@Event}", @event);

        try
        {
            // Process event
            await ProcessCashierCreated(@event);

            _logger.LogInformation("Successfully processed CashierCreated for {CashierId}", @event.CashierId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process CashierCreated for {CashierId}", @event.CashierId);
            throw; // Re-throw to trigger retry logic
        }
    }
}
```

## Development Tools

### Recommended Extensions

**Visual Studio Code**:

-   C# Dev Kit
-   REST Client
-   Docker
-   PostgreSQL Explorer

**Visual Studio**:

-   .NET Aspire Workload
-   Entity Framework Power Tools
-   ResXManager

### Debugging Tools

**Process Monitoring**:

```bash
# Monitor running .NET processes
dotnet-trace ps

# Collect traces
dotnet-trace collect -p <process-id> --format speedscope
```

**Network Debugging**:

```bash
# Monitor HTTP traffic
dotnet tool install -g dotnet-httprepl

# Navigate and test API endpoints
httprepl http://localhost:8101
```

## Best Practices

### Logging Guidelines

1. **Use Structured Logging**: Include relevant context properties
2. **Appropriate Log Levels**: Debug for development, Information for operations
3. **Avoid Sensitive Data**: Never log passwords or personal information
4. **Performance Impact**: Be mindful of logging overhead in hot paths

### Exception Handling

```csharp
public async Task<Result<Cashier>> Handle(CreateCashierCommand request)
{
    try
    {
        // Business logic
        return Result.Success(cashier);
    }
    catch (SqlException ex) when (ex.Number == 2) // Timeout
    {
        _logger.LogWarning(ex, "Database timeout creating cashier {Name}", request.Name);
        return Result.Failure("Database operation timed out");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error creating cashier {Name}", request.Name);
        return Result.Failure("An unexpected error occurred");
    }
}
```

### Testing Strategies

1. **Isolated Unit Tests**: Mock external dependencies
2. **Integration Tests**: Use real infrastructure with TestContainers
3. **End-to-End Tests**: Test complete user workflows
4. **Performance Tests**: Validate under realistic load

## Next Steps

-   Learn about [Development Setup](/guide/dev-setup) for optimal debugging environment
-   Explore [Testing Strategies](/arch/testing) for comprehensive testing approaches
-   Review [Error Handling](/arch/error-handling) patterns for robust applications
-   Understand [Background Processing](/arch/background-processing) debugging techniques

## Related Resources

-   [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
-   [OpenTelemetry for .NET](https://opentelemetry.io/docs/languages/net/)
-   [Serilog Best Practices](https://serilog.net/)
-   [TestContainers Documentation](https://testcontainers.com/)
