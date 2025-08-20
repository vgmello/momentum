# Troubleshooting Momentum Applications

This guide covers common issues you might encounter when building applications with Momentum and how to resolve them.

## Common Startup Issues

### "Assembly not found" Errors

**Symptoms:**

-   Application fails to start
-   Error messages about missing assemblies
-   Commands/queries not being discovered

**Solutions:**

1. **Mark domain assemblies correctly:**

```csharp
// In your API project Program.cs
using YourApp.Domain;

[assembly: DomainAssembly(typeof(IYourDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);
// ... rest of configuration
```

2. **Create proper domain marker interfaces:**

```csharp
// YourApp.Domain/IYourDomainAssembly.cs
namespace YourApp.Domain;

public interface IYourDomainAssembly;
```

3. **Set entry assembly manually if needed:**

```csharp
// Before calling AddServiceDefaults()
ServiceDefaultsExtensions.EntryAssembly = typeof(Program).Assembly;

builder.AddServiceDefaults();
```

### Service Discovery Issues

**Symptoms:**

-   Services not being registered
-   Dependency injection failures
-   "Service not found" exceptions

**Solutions:**

1. **Verify service defaults are added:**

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

// This must be called first
builder.AddServiceDefaults();

// Then add your custom services
builder.Services.AddScoped<IMyService, MyService>();
```

2. **Check domain assembly registration:**

```csharp
// Verify assemblies are marked correctly
[assembly: DomainAssembly(typeof(IDomainMarker))]

// Check logs for assembly discovery
logger.LogInformation("Discovered assemblies: {Assemblies}",
    DomainAssemblyAttribute.GetDomainAssemblies().Select(a => a.FullName));
```

3. **Manual service registration as fallback:**

```csharp
// If automatic discovery fails
builder.Services.AddValidatorsFromAssemblyContaining<MyValidator>();
builder.Services.AddMediatR(typeof(MyCommand));
```

## Database Connection Issues

### Connection String Problems

**Symptoms:**

-   "Cannot connect to database" errors
-   Timeout exceptions
-   Authentication failures

**Solutions:**

1. **Verify connection string format:**

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Port=5432;Database=myapp;Username=postgres;Password=password"
    }
}
```

2. **Check database availability:**

```bash
# Test PostgreSQL connection
psql -h localhost -p 5432 -U postgres -d myapp

# Check if database exists
SELECT datname FROM pg_database WHERE datname = 'myapp';
```

3. **Use connection string validation:**

```csharp
public static class DatabaseExtensions
{
    public static void ValidateConnectionString(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string not found");
        }

        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot connect to database: {ex.Message}", ex);
        }
    }
}

// In Program.cs
builder.Configuration.ValidateConnectionString();
```

### Migration Issues

**Symptoms:**

-   Database schema not up to date
-   Missing tables or columns
-   Migration execution failures

**Solutions:**

1. **Run migrations manually:**

```bash
# Using Docker Compose
docker compose up myapp-db-migrations

# Or check migration status
docker compose exec myapp-db-migrations liquibase status
```

2. **Verify migration files:**

```bash
# Check migration directory
ls -la infra/MyApp.Database/migrations/

# Validate Liquibase changelog
docker compose exec myapp-db-migrations liquibase validate
```

3. **Reset database for development:**

```bash
# Completely reset development database
docker compose down -v
docker compose up myapp-db myapp-db-migrations
```

## Validation Issues

### Validators Not Found

**Symptoms:**

-   Validation not running
-   Commands/queries not being validated
-   No validation errors when expected

**Solutions:**

1. **Check validator registration:**

```csharp
// Ensure validators are in domain assemblies
[assembly: DomainAssembly(typeof(IDomainMarker))]

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
```

2. **Manual validator registration:**

```csharp
// As fallback, register manually
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
```

3. **Debug validator discovery:**

```csharp
// Add logging to see what validators are found
var validators = builder.Services
    .Where(s => s.ServiceType.IsGenericType &&
               s.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>))
    .ToList();

foreach (var validator in validators)
{
    logger.LogInformation("Found validator: {ValidatorType} for {CommandType}",
        validator.ImplementationType, validator.ServiceType.GetGenericArguments()[0]);
}
```

### Validation Rules Not Working

**Symptoms:**

-   Validation rules are ignored
-   Unexpected validation results
-   Custom validation logic not executing

**Solutions:**

1. **Check rule syntax:**

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        // Correct
        RuleFor(x => x.Email).NotEmpty().EmailAddress();

        // Incorrect - missing property selector
        // RuleFor("Email").NotEmpty(); // Won't work
    }
}
```

2. **Test validators independently:**

```csharp
[Test]
public void Validator_InvalidEmail_ReturnsError()
{
    // Arrange
    var validator = new CreateUserValidator();
    var command = new CreateUserCommand("", "invalid-email");

    // Act
    var result = validator.Validate(command);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == "Email");
}
```

3. **Enable validation debugging:**

```csharp
// In appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "FluentValidation": "Debug",
      "Momentum.ServiceDefaults.Validation": "Debug"
    }
  }
}
```

## Messaging and Event Issues

### Events Not Being Published

**Symptoms:**

-   Integration events not appearing in Kafka
-   Event handlers not being triggered
-   No event-related logs

**Solutions:**

1. **Verify event definition:**

```csharp
[EventTopic<User>] // Ensure EventTopic attribute is present
public record UserCreated(
    [PartitionKey] Guid TenantId,
    User User
);
```

2. **Check event handler return:**

```csharp
public static async Task<(Result<User>, UserCreated?)> Handle(
    CreateUserCommand command,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // ... business logic ...

    var result = insertedUser.ToModel();
    var createdEvent = new UserCreated(result.TenantId, result); // Must return event

    return (result, createdEvent); // Framework publishes the event
}
```

3. **Check Kafka configuration:**

```json
{
    "Kafka": {
        "BootstrapServers": "localhost:9092",
        "EnableAutoCommit": false,
        "GroupId": "myapp-consumer-group"
    }
}
```

4. **Verify Kafka is running:**

```bash
# Check Kafka container
docker compose ps kafka

# List Kafka topics
docker compose exec kafka kafka-topics --list --bootstrap-server localhost:9092

# Check topic contents
docker compose exec kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic dev.myapp.public.users.v1 \
  --from-beginning
```

### Event Handlers Not Executing

**Symptoms:**

-   Events are published but handlers don't run
-   No handler logs
-   Events accumulate in topics

**Solutions:**

1. **Check handler registration:**

```csharp
public static class UserCreatedHandler
{
    public static async Task Handle(
        UserCreated userCreated,
        ILogger<UserCreatedHandler> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing UserCreated event for {UserId}", userCreated.User.Id);
        // ... handler logic ...
    }
}

// Handlers are automatically discovered from domain assemblies
[assembly: DomainAssembly(typeof(IDomainMarker))]
```

2. **Check consumer group configuration:**

```csharp
// In appsettings.json
{
  "Wolverine": {
    "Kafka": {
      "ConsumerGroupId": "myapp-handlers" // Should be unique per service
    }
  }
}
```

3. **Monitor handler errors:**

```csharp
public static async Task Handle(
    UserCreated userCreated,
    ILogger<UserCreatedHandler> logger,
    CancellationToken cancellationToken)
{
    try
    {
        // Handler logic
        await DoSomethingAsync(userCreated, cancellationToken);

        logger.LogInformation("Successfully processed UserCreated event");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to process UserCreated event");
        throw; // Re-throw to trigger retry
    }
}
```

## Performance Issues

### Slow Database Operations

**Symptoms:**

-   High response times
-   Database timeouts
-   Connection pool exhaustion

**Solutions:**

1. **Add database indexes:**

```sql
-- Check for missing indexes
EXPLAIN ANALYZE SELECT * FROM cashiers WHERE tenant_id = $1 AND email = $2;

-- Add appropriate indexes
CREATE INDEX CONCURRENTLY idx_cashiers_tenant_email
ON cashiers (tenant_id, email);
```

2. **Monitor query performance:**

```csharp
public class QueryPerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<QueryPerformanceMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 1000) // Log slow requests
            {
                _logger.LogWarning("Slow request: {Method} {Path} took {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
```

3. **Optimize queries:**

```csharp
// Use projection to reduce data transfer
var summary = await db.Users
    .Where(u => u.TenantId == tenantId)
    .Select(u => new UserSummary // Only select needed fields
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email
    })
    .ToListAsync(cancellationToken);

// Use pagination for large result sets
var pagedUsers = await db.Users
    .Where(u => u.TenantId == tenantId)
    .OrderBy(u => u.Name)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(cancellationToken);
```

### High Memory Usage

**Symptoms:**

-   OutOfMemoryException
-   Garbage collection pressure
-   Application crashes

**Solutions:**

1. **Use streaming for large result sets:**

```csharp
public static async IAsyncEnumerable<User> GetUsersStreamAsync(
    AppDomainDb db,
    Guid tenantId,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await foreach (var user in db.Users
        .Where(u => u.TenantId == tenantId)
        .AsAsyncEnumerable()
        .WithCancellation(cancellationToken))
    {
        yield return user.ToModel();
    }
}
```

2. **Dispose resources properly:**

```csharp
public static async Task<Result<ProcessingResult>> Handle(
    LargeProcessingCommand command,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    using var transaction = await db.BeginTransactionAsync(cancellationToken);

    try
    {
        // Process data in chunks
        const int batchSize = 1000;
        var processed = 0;

        var query = db.LargeDataSet.Where(d => d.TenantId == command.TenantId);

        await foreach (var batch in query
            .AsAsyncEnumerable()
            .Chunk(batchSize)
            .WithCancellation(cancellationToken))
        {
            await ProcessBatch(batch, db, cancellationToken);
            processed += batch.Length;

            // Force garbage collection periodically
            if (processed % 10000 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return new ProcessingResult { ProcessedCount = processed };
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

3. **Monitor memory usage:**

```csharp
public class MemoryMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MemoryMonitoringMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var beforeGC = GC.GetTotalMemory(false);
        var beforeWorking = Environment.WorkingSet;

        try
        {
            await _next(context);
        }
        finally
        {
            var afterGC = GC.GetTotalMemory(false);
            var afterWorking = Environment.WorkingSet;
            var memoryDelta = afterGC - beforeGC;
            var workingDelta = afterWorking - beforeWorking;

            if (memoryDelta > 10_000_000) // 10MB
            {
                _logger.LogWarning("High memory allocation in request {Path}: GC={GCDelta}MB, Working={WorkingDelta}MB",
                    context.Request.Path,
                    memoryDelta / 1024 / 1024,
                    workingDelta / 1024 / 1024);
            }
        }
    }
}
```

## Authentication and Authorization Issues

### JWT Token Problems

**Symptoms:**

-   401 Unauthorized responses
-   Token validation failures
-   Claims not found

**Solutions:**

1. **Verify JWT configuration:**

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<JwtBearerHandler>>();

                logger.LogError(context.Exception, "JWT authentication failed: {Error}",
                    context.Exception.Message);

                return Task.CompletedTask;
            }
        };
    });
```

2. **Debug token validation:**

```csharp
public class TokenDebuggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenDebuggingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            var token = context.Request.Headers.Authorization.FirstOrDefault();
            _logger.LogDebug("Processing request with token: {Token}",
                token?.Substring(0, Math.Min(token.Length, 50)) + "...");
        }
        else
        {
            _logger.LogDebug("Processing request without authorization header");
        }

        await _next(context);

        if (!context.User.Identity?.IsAuthenticated == true)
        {
            _logger.LogWarning("Request completed without authentication for path: {Path}",
                context.Request.Path);
        }
    }
}
```

3. **Test token manually:**

```bash
# Decode JWT token
echo "YOUR_JWT_TOKEN" | base64 -d

# Test API endpoint
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" https://localhost:5001/api/users
```

### Authorization Policy Issues

**Symptoms:**

-   403 Forbidden responses
-   Authorization policies not working
-   Claims-based authorization failures

**Solutions:**

1. **Check policy configuration:**

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireClaim("role", "admin"));

    options.AddPolicy("SameTenant", policy =>
        policy.AddRequirements(new TenantRequirement()));
});

// Register authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, TenantAuthorizationHandler>();
```

2. **Debug authorization:**

```csharp
public class AuthorizationLoggingHandler : IAuthorizationHandler
{
    private readonly ILogger<AuthorizationLoggingHandler> _logger;

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        _logger.LogDebug("Evaluating authorization for user {UserId} with claims: {Claims}",
            context.User.FindFirst("sub")?.Value,
            string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}")));

        foreach (var requirement in context.Requirements)
        {
            _logger.LogDebug("Checking requirement: {RequirementType}", requirement.GetType().Name);
        }

        return Task.CompletedTask;
    }
}
```

## Testing Issues

### Integration Test Failures

**Symptoms:**

-   Tests fail in CI but pass locally
-   Database-related test failures
-   Service dependency issues

**Solutions:**

1. **Use test containers properly:**

```csharp
public class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainer _dbContainer;
    protected readonly KafkaContainer _kafkaContainer;
    protected string ConnectionString => _dbContainer.GetConnectionString();

    public IntegrationTestBase()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("test_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        _kafkaContainer = new KafkaBuilder()
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _kafkaContainer.StartAsync();

        // Run migrations
        await RunMigrations();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _kafkaContainer.StopAsync();
    }
}
```

2. **Isolate tests properly:**

```csharp
[Fact]
public async Task CreateUser_ValidData_ReturnsCreatedUser()
{
    // Arrange - use unique tenant ID for isolation
    var tenantId = Guid.NewGuid();

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();

    // Clean up before test
    await db.Users.Where(u => u.TenantId == tenantId).DeleteAsync();

    var command = new CreateUserCommand(tenantId, "John Doe", "john@example.com");

    // Act
    var result = await SendAsync(command);

    // Assert
    result.IsSuccess.Should().BeTrue();

    // Verify in database
    var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == result.Value.Id);
    user.Should().NotBeNull();
}
```

## Docker and Deployment Issues

### Container Startup Problems

**Symptoms:**

-   Containers fail to start
-   Port binding issues
-   Volume mounting problems

**Solutions:**

1. **Check Docker Compose configuration:**

```yaml
version: "3.8"
services:
    app:
        build: .
        ports:
            - "5000:8080" # Map host:container ports correctly
        environment:
            - ASPNETCORE_ENVIRONMENT=Development
            - ConnectionStrings__DefaultConnection=Host=db;Database=myapp;Username=postgres;Password=password
        depends_on:
            - db
        networks:
            - myapp-network

    db:
        image: postgres:15
        environment:
            POSTGRES_DB: myapp
            POSTGRES_USER: postgres
            POSTGRES_PASSWORD: password
        ports:
            - "5432:5432"
        volumes:
            - postgres_data:/var/lib/postgresql/data
        networks:
            - myapp-network

volumes:
    postgres_data:

networks:
    myapp-network:
        driver: bridge
```

2. **Debug container issues:**

```bash
# Check container logs
docker compose logs app
docker compose logs db

# Exec into container for debugging
docker compose exec app bash

# Check network connectivity
docker compose exec app ping db
docker compose exec app curl http://db:5432

# Check port binding
netstat -tlnp | grep 5000
```

### Health Check Failures

**Symptoms:**

-   Health check endpoints return unhealthy
-   Load balancer removes instances
-   Kubernetes pods fail readiness checks

**Solutions:**

1. **Improve health check implementation:**

```csharp
public class ComprehensiveHealthCheck : IHealthCheck
{
    private readonly AppDomainDb _db;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<ComprehensiveHealthCheck> _logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var checks = new List<(string Name, Task<HealthCheckResult> Check)>
        {
            ("Database", CheckDatabaseAsync(cancellationToken)),
            ("Messaging", CheckMessagingAsync(cancellationToken)),
            ("External Services", CheckExternalServicesAsync(cancellationToken))
        };

        var results = await Task.WhenAll(checks.Select(c => c.Check));
        var failed = results.Where(r => r.Status != HealthStatus.Healthy).ToList();

        if (!failed.Any())
        {
            return HealthCheckResult.Healthy("All checks passed");
        }

        var errors = string.Join("; ", failed.Select(f => f.Description));

        if (failed.Any(f => f.Status == HealthStatus.Unhealthy))
        {
            return HealthCheckResult.Unhealthy($"Critical failures: {errors}");
        }

        return HealthCheckResult.Degraded($"Non-critical issues: {errors}");
    }
}
```

2. **Configure appropriate timeouts:**

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<ComprehensiveHealthCheck>("comprehensive",
        timeout: TimeSpan.FromSeconds(30),
        tags: ["ready"])
    .AddCheck("quick", () => HealthCheckResult.Healthy(),
        tags: ["live"]);

// Different endpoints with different timeouts
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    Timeout = TimeSpan.FromSeconds(30)
});
```

## Monitoring and Observability Issues

### Missing Telemetry Data

**Symptoms:**

-   No traces in monitoring system
-   Missing metrics
-   Logs not appearing

**Solutions:**

1. **Verify OpenTelemetry configuration:**

```csharp
// Check exporter configuration
builder.Services.ConfigureOpenTelemetryTracingBuilder(tracing =>
{
    tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddJaegerExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:JaegerEndpoint"]!);
        })
        .AddConsoleExporter(); // For debugging
});
```

2. **Add custom instrumentation:**

```csharp
public static class TelemetryExtensions
{
    private static readonly ActivitySource ActivitySource = new("MyApp");

    public static async Task<T> TraceAsync<T>(
        this Task<T> task,
        string operationName,
        Action<Activity?>? configure = null)
    {
        using var activity = ActivitySource.StartActivity(operationName);
        configure?.Invoke(activity);

        try
        {
            var result = await task;
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

3. **Debug telemetry pipeline:**

```csharp
// Add console exporter for debugging
builder.Services.ConfigureOpenTelemetryTracingBuilder(tracing =>
{
    tracing.AddConsoleExporter();
});

// Check for instrumentation
var instrumentation = builder.Services
    .Where(s => s.ServiceType.Name.Contains("Instrumentation"))
    .ToList();

foreach (var instr in instrumentation)
{
    logger.LogInformation("Registered instrumentation: {Type}", instr.ImplementationType);
}
```

## Best Practices for Troubleshooting

### Logging Best Practices

1. **Use structured logging:**

```csharp
// Good
logger.LogInformation("Processing order {OrderId} for customer {CustomerId}",
    orderId, customerId);

// Bad
logger.LogInformation($"Processing order {orderId} for customer {customerId}");
```

2. **Include correlation IDs:**

```csharp
public class CorrelationMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        await next(context);
    }
}
```

### Error Handling Best Practices

1. **Provide actionable error messages:**

```csharp
public static Result<User> ValidateUser(CreateUserCommand command)
{
    var errors = new List<ValidationFailure>();

    if (string.IsNullOrEmpty(command.Email))
    {
        errors.Add(new ValidationFailure("Email", "Email is required"));
    }
    else if (!IsValidEmail(command.Email))
    {
        errors.Add(new ValidationFailure("Email", "Please provide a valid email address"));
    }

    return errors.Any() ? errors : Result<User>.Success(/* user */);
}
```

2. **Log exceptions with context:**

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Failed to process command {CommandType} for tenant {TenantId}. Context: {Context}",
        command.GetType().Name,
        command.TenantId,
        new { UserId = context.User?.FindFirst("sub")?.Value, RequestId = context.TraceIdentifier });

    throw;
}
```

### Testing and Debugging

1. **Use debugger effectively:**

```csharp
[Conditional("DEBUG")]
public static void DebugBreakIf(bool condition, string message = "")
{
    if (condition)
    {
        Debug.WriteLine($"Debug break: {message}");
        Debugger.Break();
    }
}

// Usage
DebugBreakIf(user.TenantId != expectedTenantId, "Tenant mismatch detected");
```

2. **Create debugging endpoints:**

```csharp
#if DEBUG
app.MapGet("/debug/health-detailed", async (
    IServiceProvider services) =>
{
    var healthCheckService = services.GetRequiredService<HealthCheckService>();
    var result = await healthCheckService.CheckHealthAsync();

    return Results.Ok(new
    {
        Status = result.Status.ToString(),
        Checks = result.Entries.ToDictionary(
            kvp => kvp.Key,
            kvp => new
            {
                Status = kvp.Value.Status.ToString(),
                Description = kvp.Value.Description,
                Exception = kvp.Value.Exception?.Message,
                Duration = kvp.Value.Duration.TotalMilliseconds
            })
    });
});
#endif
```

## Quick Reference: Common Error Patterns

### Error Message Lookup Table

| Error Message Pattern    | Category       | Quick Fix                                     |
| ------------------------ | -------------- | --------------------------------------------- |
| "Assembly not found"     | Startup        | Add `[assembly: DomainAssembly(...)]`         |
| "Service not registered" | DI             | Check service registration order              |
| "Connection refused"     | Database       | Verify database is running and accessible     |
| "Topic not found"        | Messaging      | Check Kafka topic auto-creation settings      |
| "Validation failed"      | Business Logic | Check FluentValidation rules                  |
| "Unauthorized"           | Security       | Verify JWT configuration and claims           |
| "Timeout"                | Performance    | Check database indexes and query performance  |
| "Memory" issues          | Resources      | Profile application and optimize memory usage |

### Environment-Specific Troubleshooting

#### Development Environment

```bash
# Quick development reset
docker compose down -v  # Remove all containers and volumes
docker compose up -d postgres kafka  # Start infrastructure
dotnet run --project src/YourApp.Api  # Start application
```

#### Production Environment

```bash
# Check application health
curl -f http://localhost:5000/health || echo "Health check failed"

# Check resource usage
free -m  # Memory
df -h    # Disk space
top      # CPU and process information

# Check logs
journalctl -u your-app-service --since "1 hour ago"
```

#### Docker Environment

```bash
# Container diagnostics
docker ps -a                    # List all containers
docker logs container-name      # View container logs
docker exec -it container bash  # Access container shell
docker stats                    # Real-time resource usage
```

## Getting Help and Resources

### Self-Service Resources

1. **Momentum Documentation:**

    - [Getting Started Guide](./getting-started) - Basic setup and configuration
    - [CQRS Guide](./cqrs/) - Commands, queries, and handlers
    - [Best Practices](./best-practices) - Production-ready patterns
    - [Testing Guide](./testing) - Comprehensive testing strategies

2. **Diagnostic Tools:**

    - `dotnet-trace` for performance issues
    - `dotnet-dump` for memory issues
    - `dotnet-counters` for real-time metrics
    - `dotnet-gcdump` for garbage collection analysis

3. **Framework Documentation:**
    - ASP.NET Core documentation
    - Entity Framework Core documentation
    - OpenTelemetry documentation
    - Wolverine messaging documentation

### Community and Support

-   **GitHub Issues** - Report bugs and feature requests
-   **Stack Overflow** - Community Q&A with `momentum-dotnet` tag
-   **Discord/Slack** - Real-time community support

### Performance Troubleshooting Checklist

-   [ ] Database indexes are properly configured
-   [ ] Connection pooling is optimized
-   [ ] Memory usage is within acceptable limits
-   [ ] CPU usage is not consistently high
-   [ ] Network latency is acceptable
-   [ ] Garbage collection is not excessive
-   [ ] Thread pool is not exhausted
-   [ ] External service calls are optimized

> **Remember**: Always include error messages, configuration (redacted), logs, and steps to reproduce when seeking help.
