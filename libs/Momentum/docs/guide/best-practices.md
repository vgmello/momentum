# Best Practices for Momentum Applications

This guide outlines proven practices for building robust, scalable, and maintainable applications with Momentum. These practices are derived from real-world experience and production deployments, emphasizing Momentum's template-driven approach and real-world mirroring philosophy.

## Core Principles

### Real-World Mirroring

Structure your code to directly correspond to business operations:

-   **Commands represent business actions**: If your business can "Create Order" or "Process Payment", your code should have `CreateOrderCommand` and `ProcessPaymentCommand`
-   **Queries represent business information needs**: If your business needs to "Find Customer" or "Calculate Total", use `FindCustomerQuery` and `CalculateTotalQuery`
-   **Avoid technical abstractions**: Don't create repositories, services, or managers unless they mirror real business roles
-   **Use business language**: Non-technical stakeholders should understand your code structure

### Template-Driven Development

-   **Copy and customize**: Take patterns from Momentum and adapt them to your specific needs
-   **No framework lock-in**: You own the code completely and can modify patterns as needed
-   **Maintain patterns**: Keep consistent approaches across your codebase for maintainability

## Architecture and Design

### Domain-Driven Design

#### Organize by Business Domains

Structure your codebase around business domains rather than technical layers:

```
src/
├── AppDomain/
│   ├── Cashiers/           # Business domain
│   │   ├── Commands/       # Write operations
│   │   ├── Queries/        # Read operations
│   │   ├── Contracts/      # External contracts
│   │   └── Data/           # Domain data access
│   ├── Invoices/           # Another business domain
│   └── Core/               # Shared domain logic
├── AppDomain.Api/          # HTTP endpoints
└── AppDomain.BackOffice/   # Background processing
```

#### Keep Domain Logic Pure

Avoid infrastructure concerns in domain logic:

```csharp
// Good: Pure business logic
public static class CreateCashierCommandHandler
{
    public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
        CreateCashierCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // Business rules and validation
        if (IsEmailAlreadyInUse(command.Email))
        {
            return (Result<Cashier>.Failure("Email already in use"), null);
        }

        var dbCommand = CreateInsertCommand(command);
        var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        return (insertedCashier.ToModel(), new CashierCreated(/* ... */));
    }
}

// Bad: Infrastructure mixed with business logic
public static class CreateCashierCommandHandler
{
    public static async Task<Result<Cashier>> Handle(
        CreateCashierCommand command,
        HttpClient httpClient, // Infrastructure concern
        ILogger logger,        // Infrastructure concern
        CancellationToken cancellationToken)
    {
        // Don't mix HTTP calls, logging, etc. with business logic
    }
}
```

### Command Query Responsibility Segregation (CQRS)

#### Separate Read and Write Models

Use different models for commands (writes) and queries (reads):

```csharp
// Write model - rich with business rules
public class Cashier
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }

    public void UpdateEmail(string newEmail)
    {
        if (!IsValidEmail(newEmail))
            throw new InvalidOperationException("Invalid email");

        Email = newEmail;
    }
}

// Read model - optimized for display
public class CashierSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime LastActivityDate { get; set; }
}
```

#### Optimize Queries Independently

Tailor queries to specific use cases:

```csharp
// List view - minimal data
public record GetCashierSummariesQuery(Guid TenantId, int Page, int PageSize)
    : IQuery<Result<PagedResult<CashierSummary>>>;

// Detail view - complete data
public record GetCashierDetailQuery(Guid TenantId, Guid CashierId)
    : IQuery<Result<CashierDetail>>;

// Search - specific fields
public record SearchCashiersQuery(Guid TenantId, string SearchTerm)
    : IQuery<Result<List<CashierSearchResult>>>;
```

### Event-Driven Architecture

#### Design Events for Consumers

Think about who will consume your events and what they need:

```csharp
[EventTopic<Cashier>]
public record CashierCreated(
    [PartitionKey] Guid TenantId,
    Cashier Cashier,
    DateTime CreatedAt,
    string CreatedBy,
    // Include relevant business context
    string Department,
    List<string> InitialPermissions
);
```

#### Use Semantic Versioning for Events

Plan for event evolution:

```csharp
// Version 1
[EventTopic<Cashier>(Version = "v1")]
public record CashierCreatedV1(
    [PartitionKey] Guid TenantId,
    Guid CashierId,
    string Name,
    string Email
);

// Version 2 - added fields
[EventTopic<Cashier>(Version = "v2")]
public record CashierCreatedV2(
    [PartitionKey] Guid TenantId,
    Guid CashierId,
    string Name,
    string Email,
    string Department,    // New field
    DateTime CreatedAt    // New field
);
```

#### Handle Event Ordering

Use partition keys to ensure proper ordering:

```csharp
[EventTopic<CashierAggregate>]
public record CashierUpdated(
    [PartitionKey(Order = 0)] Guid TenantId,     // Tenant-level partitioning
    [PartitionKey(Order = 1)] Guid CashierId,    // Entity-level ordering
    CashierAggregate Cashier
);
```

## Data Management

### Database Design

#### Use Appropriate Data Types

Choose the right PostgreSQL data types:

```csharp
public class CashierEntity
{
    public Guid CashierId { get; set; }      // uuid - indexed
    public Guid TenantId { get; set; }       // uuid - indexed
    public string Name { get; set; }         // varchar(100)
    public string Email { get; set; }        // varchar(255)
    public decimal Salary { get; set; }      // decimal(10,2)
    public DateTime CreatedDateUtc { get; set; }  // timestamp with time zone
    public bool IsActive { get; set; }       // boolean
    public JsonDocument Metadata { get; set; } // jsonb for flexible data
}
```

#### Create Proper Indexes

Index based on query patterns:

```sql
-- Primary key (automatic)
CREATE UNIQUE INDEX pk_cashiers ON cashiers (cashier_id);

-- Tenant queries (most common)
CREATE INDEX idx_cashiers_tenant_active ON cashiers (tenant_id, is_active);

-- Email lookups
CREATE UNIQUE INDEX idx_cashiers_tenant_email ON cashiers (tenant_id, email);

-- Name searches
CREATE INDEX idx_cashiers_name_gin ON cashiers USING gin (name gin_trgm_ops);

-- JSON queries
CREATE INDEX idx_cashiers_metadata_gin ON cashiers USING gin (metadata);
```

#### Use Snake Case Naming

Follow PostgreSQL conventions:

```csharp
[Table("cashiers")]
public class CashierEntity
{
    [Column("cashier_id")]
    public Guid CashierId { get; set; }

    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Column("created_date_utc")]
    public DateTime CreatedDateUtc { get; set; }
}
```

### Transaction Management

#### Keep Transactions Short

Minimize transaction duration:

```csharp
// Good: Short transaction
public static async Task<Result<Cashier>> Handle(
    CreateCashierCommand command,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // All database work in one quick transaction
    var dbCommand = CreateInsertCommand(command);
    var result = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

    return result.ToModel();
}

// Bad: Long transaction with external calls
public static async Task<Result<Cashier>> Handle(
    CreateCashierCommand command,
    IMessageBus messaging,
    HttpClient httpClient, // External dependency
    CancellationToken cancellationToken)
{
    // Don't include slow external calls in transactions
    using var transaction = await db.BeginTransactionAsync();

    // Database work
    var cashier = await CreateCashierAsync(command);

    // This makes the transaction long and prone to deadlocks
    await httpClient.PostAsync("external-service", /* data */);

    await transaction.CommitAsync();
}
```

#### Use Saga Pattern for Complex Workflows

For multi-service transactions:

```csharp
public class CreateCashierSaga
{
    public async Task<Result> Execute(CreateCashierCommand command)
    {
        var steps = new List<ISagaStep>
        {
            new CreateCashierStep(command),
            new SendWelcomeEmailStep(command.Email),
            new ProvisionAccessStep(command.CashierId),
            new NotifyManagerStep(command.ManagerId)
        };

        var compensations = new List<ICompensation>();

        foreach (var step in steps)
        {
            try
            {
                var compensation = await step.ExecuteAsync();
                compensations.Add(compensation);
            }
            catch (Exception ex)
            {
                // Compensate in reverse order
                foreach (var compensation in compensations.Reverse())
                {
                    await compensation.CompensateAsync();
                }

                return Result.Failure($"Saga failed at step {step.GetType().Name}: {ex.Message}");
            }
        }

        return Result.Success();
    }
}
```

## Performance Optimization

### Query Optimization

#### Use Projections

Only select data you need:

```csharp
// Good: Projection
var summaries = await db.Cashiers
    .Where(c => c.TenantId == tenantId)
    .Select(c => new CashierSummary
    {
        Id = c.CashierId,
        Name = c.Name,
        Email = c.Email
        // Only the fields you need
    })
    .ToListAsync(cancellationToken);

// Bad: Loading full entities
var cashiers = await db.Cashiers
    .Where(c => c.TenantId == tenantId)
    .ToListAsync(cancellationToken);

var summaries = cashiers.Select(c => c.ToSummary()).ToList(); // Wasteful
```

#### Implement Pagination

Always paginate large result sets:

```csharp
public record GetCashiersQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20) // Default reasonable page size
    : IQuery<Result<PagedResult<Cashier>>>;

public static async Task<Result<PagedResult<Cashier>>> Handle(
    GetCashiersQuery query,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    // Validate pagination parameters
    if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
    {
        return Result<PagedResult<Cashier>>.Failure("Invalid pagination parameters");
    }

    var skip = (query.Page - 1) * query.PageSize;

    var cashiersQuery = db.Cashiers
        .Where(c => c.TenantId == query.TenantId)
        .OrderBy(c => c.Name);

    var totalCount = await cashiersQuery.CountAsync(cancellationToken);

    var items = await cashiersQuery
        .Skip(skip)
        .Take(query.PageSize)
        .Select(c => c.ToModel())
        .ToListAsync(cancellationToken);

    return new PagedResult<Cashier>
    {
        Items = items,
        Page = query.Page,
        PageSize = query.PageSize,
        TotalCount = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
    };
}
```

### Caching Strategies

#### Cache Frequently Accessed Data

```csharp
public static class GetCashierQueryHandler
{
    public static async Task<Result<Cashier>> Handle(
        GetCashierQuery query,
        AppDomainDb db,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"cashier_{query.TenantId}_{query.Id}";

        if (cache.TryGetValue(cacheKey, out Cashier? cachedCashier))
        {
            return cachedCashier!;
        }

        var cashier = await db.Cashiers
            .FirstOrDefaultAsync(c =>
                c.TenantId == query.TenantId &&
                c.CashierId == query.Id,
                cancellationToken);

        if (cashier != null)
        {
            var result = cashier.ToModel();

            // Cache for 5 minutes with size limit
            cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                SlidingExpiration = TimeSpan.FromMinutes(1),
                Size = 1,
                Priority = CacheItemPriority.Normal
            });

            return result;
        }

        return new List<ValidationFailure> { new("Id", "Cashier not found") };
    }
}
```

#### Invalidate Cache Appropriately

```csharp
public static class UpdateCashierCommandHandler
{
    public static async Task<(Result<Cashier>, CashierUpdated?)> Handle(
        UpdateCashierCommand command,
        IMessageBus messaging,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        var result = await messaging.InvokeCommandAsync(
            new DbCommand(command.ToEntity()),
            cancellationToken);

        if (result != null)
        {
            // Invalidate cache
            var cacheKey = $"cashier_{command.TenantId}_{command.Id}";
            cache.Remove(cacheKey);

            var model = result.ToModel();
            var updatedEvent = new CashierUpdated(command.TenantId, model);

            return (model, updatedEvent);
        }

        return (Result<Cashier>.Failure("Update failed"), null);
    }
}
```

### Async Programming

#### Use ConfigureAwait(false)

In library code, avoid deadlocks:

```csharp
// Good: In library/service code
public static async Task<Result<Cashier>> Handle(
    GetCashierQuery query,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    var cashier = await db.Cashiers
        .FirstOrDefaultAsync(c => c.TenantId == query.TenantId, cancellationToken)
        .ConfigureAwait(false); // Avoid deadlocks in library code

    return cashier?.ToModel() ??
           new List<ValidationFailure> { new("Id", "Not found") };
}

// Note: In ASP.NET Core applications, ConfigureAwait(false) is often unnecessary
// due to the async context, but it's still a good practice in libraries
```

#### Avoid Async Void

Use async Task instead:

```csharp
// Good
public static async Task Handle(
    UserCreated userCreated,
    IEmailService emailService,
    CancellationToken cancellationToken)
{
    await emailService.SendWelcomeEmailAsync(
        userCreated.User.Email,
        cancellationToken);
}

// Bad: Can't be awaited or handle exceptions properly
public static async void Handle(UserCreated userCreated, IEmailService emailService)
{
    await emailService.SendWelcomeEmailAsync(userCreated.User.Email);
}
```

## Security

### Authentication and Authorization

#### Implement Multi-Tenant Security

```csharp
public class TenantAuthorizationHandler : AuthorizationHandler<TenantRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRequirement requirement)
    {
        var userTenantId = context.User.FindFirst("tenant_id")?.Value;

        if (userTenantId != null &&
            Guid.Parse(userTenantId) == requirement.TenantId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Usage in commands
public record CreateCashierCommand(
    [TenantId] Guid TenantId, // Will be validated against user's tenant
    string Name,
    string Email) : ICommand<Result<Cashier>>;
```

#### Validate Input Thoroughly

```csharp
public class CreateCashierValidator : AbstractValidator<CreateCashierCommand>
{
    public CreateCashierValidator()
    {
        RuleFor(c => c.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .Length(2, 100)
            .WithMessage("Name must be between 2 and 100 characters")
            .Matches("^[a-zA-Z\\s\\-']+$")
            .WithMessage("Name contains invalid characters");

        RuleFor(c => c.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(255)
            .WithMessage("Email cannot exceed 255 characters");
    }
}
```

### Data Protection

#### Encrypt Sensitive Data

```csharp
public class EncryptedCashierEntity
{
    public Guid CashierId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;

    [Encrypted] // Custom attribute for encryption
    public string Email { get; set; } = string.Empty;

    [Encrypted]
    public string SocialSecurityNumber { get; set; } = string.Empty;

    public DateTime CreatedDateUtc { get; set; }
}

public class EncryptionInterceptor : SaveChangesInterceptor
{
    private readonly IDataProtectionProvider _dataProtection;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        EncryptMarkedProperties(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void EncryptMarkedProperties(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var encryptedProperties = entry.Entity.GetType()
                .GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(EncryptedAttribute), false).Any());

            foreach (var property in encryptedProperties)
            {
                var value = property.GetValue(entry.Entity)?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    var protector = _dataProtection.CreateProtector("EntityEncryption");
                    var encryptedValue = protector.Protect(value);
                    property.SetValue(entry.Entity, encryptedValue);
                }
            }
        }
    }
}
```

## Error Handling and Resilience

### Comprehensive Error Handling

#### Use Result Pattern Consistently

```csharp
public static class Result
{
    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(string error) => new(default, false, new[] { error });
    public static Result<T> Failure<T>(IEnumerable<string> errors) => new(default, false, errors);
}

public class Result<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IEnumerable<string> Errors { get; }

    public Result(T? value, bool isSuccess, IEnumerable<string>? errors)
    {
        Value = value;
        IsSuccess = isSuccess;
        Errors = errors ?? Enumerable.Empty<string>();
    }

    public static implicit operator Result<T>(T value) => Result.Success(value);
    public static implicit operator Result<T>(string error) => Result.Failure<T>(error);
    public static implicit operator Result<T>(List<ValidationFailure> errors) =>
        Result.Failure<T>(errors.Select(e => e.ErrorMessage));
}
```

#### Handle Specific Exception Types

```csharp
public static async Task<Result<Cashier>> Handle(
    CreateCashierCommand command,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    try
    {
        var cashier = new CashierEntity { /* ... */ };
        var result = await db.Cashiers.InsertWithOutputAsync(cashier, token: cancellationToken);

        return result.ToModel();
    }
    catch (PostgresException ex) when (ex.SqlState == "23505") // Unique violation
    {
        return "A cashier with this email already exists";
    }
    catch (PostgresException ex) when (ex.SqlState == "23503") // Foreign key violation
    {
        return "Invalid tenant ID";
    }
    catch (OperationCanceledException)
    {
        return "Operation was cancelled";
    }
    catch (TimeoutException)
    {
        return "Database operation timed out";
    }
    catch (Exception ex)
    {
        // Log unexpected exceptions
        logger.LogError(ex, "Unexpected error creating cashier for tenant {TenantId}", command.TenantId);
        return "An unexpected error occurred";
    }
}
```

### Retry Policies

#### Implement Retry with Exponential Backoff

```csharp
public static class RetryPolicy
{
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        CancellationToken cancellationToken = default)
    {
        var delay = baseDelay ?? TimeSpan.FromMilliseconds(100);
        var attempt = 0;

        while (true)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (ShouldRetry(ex, attempt, maxRetries))
            {
                attempt++;
                var delayTime = TimeSpan.FromMilliseconds(
                    delay.TotalMilliseconds * Math.Pow(2, attempt - 1));

                await Task.Delay(delayTime, cancellationToken);
            }
        }
    }

    private static bool ShouldRetry(Exception exception, int attempt, int maxRetries)
    {
        if (attempt >= maxRetries) return false;

        return exception switch
        {
            TimeoutException => true,
            PostgresException pgEx when pgEx.IsTransient => true,
            HttpRequestException httpEx when IsRetryableHttpError(httpEx) => true,
            _ => false
        };
    }
}
```

## Testing

### Unit Testing

#### Test Business Logic Separately

```csharp
[Test]
public async Task Handle_ValidCommand_ReturnsSuccessResult()
{
    // Arrange
    var command = new CreateCashierCommand(
        Guid.NewGuid(),
        "John Doe",
        "john@example.com");

    var mockMessaging = new Mock<IMessageBus>();
    mockMessaging
        .Setup(m => m.InvokeCommandAsync(
            It.IsAny<CreateCashierCommandHandler.DbCommand>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new CashierEntity
        {
            CashierId = Guid.NewGuid(),
            TenantId = command.TenantId,
            Name = command.Name,
            Email = command.Email,
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        });

    // Act
    var (result, integrationEvent) = await CreateCashierCommandHandler.Handle(
        command, mockMessaging.Object, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Name.Should().Be(command.Name);
    result.Value.Email.Should().Be(command.Email);
    integrationEvent.Should().NotBeNull();
    integrationEvent!.Cashier.Name.Should().Be(command.Name);
}
```

#### Test Validation Rules

```csharp
[Test]
public void Validator_EmptyName_ReturnsValidationError()
{
    // Arrange
    var validator = new CreateCashierValidator();
    var command = new CreateCashierCommand(Guid.NewGuid(), "", "john@example.com");

    // Act
    var result = validator.Validate(command);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Name));
    result.Errors.First(e => e.PropertyName == nameof(command.Name))
        .ErrorMessage.Should().Be("Name is required");
}
```

### Integration Testing

#### Use Test Containers

```csharp
public class CashierIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public CashierIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateCashier_ValidData_PersistsToDatabase()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();

        var tenantId = Guid.NewGuid();
        var command = new CreateCashierCommand(tenantId, "John Doe", "john@example.com");

        // Act
        var (result, integrationEvent) = await messageBus.InvokeAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify in database
        var cashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.CashierId == result.Value.Id);

        cashier.Should().NotBeNull();
        cashier!.Name.Should().Be("John Doe");
        cashier.Email.Should().Be("john@example.com");

        // Verify event
        integrationEvent.Should().NotBeNull();
        integrationEvent!.Cashier.Id.Should().Be(result.Value.Id);
    }
}

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    public IServiceProvider ServiceProvider { get; private set; } = default!;

    public TestDatabaseFixture()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("test_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var services = new ServiceCollection();
        services.AddDbContext<AppDomainDb>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        ServiceProvider = services.BuildServiceProvider();

        // Run migrations
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
```

## Monitoring and Observability

### Structured Logging

#### Use Consistent Log Patterns

```csharp
public static class LoggerExtensions
{
    private static readonly Action<ILogger, string, Guid, Exception?> _commandStarting =
        LoggerMessage.Define<string, Guid>(
            LogLevel.Information,
            new EventId(1001, "CommandStarting"),
            "Starting command {CommandName} for tenant {TenantId}");

    private static readonly Action<ILogger, string, Guid, long, Exception?> _commandCompleted =
        LoggerMessage.Define<string, Guid, long>(
            LogLevel.Information,
            new EventId(1002, "CommandCompleted"),
            "Completed command {CommandName} for tenant {TenantId} in {ElapsedMs}ms");

    public static void LogCommandStarting(this ILogger logger, string commandName, Guid tenantId)
        => _commandStarting(logger, commandName, tenantId, null);

    public static void LogCommandCompleted(this ILogger logger, string commandName, Guid tenantId, long elapsedMs)
        => _commandCompleted(logger, commandName, tenantId, elapsedMs, null);
}
```

### Custom Metrics

#### Track Business Metrics

```csharp
public class BusinessMetrics
{
    private readonly Counter<int> _cashiersCreated;
    private readonly Histogram<double> _commandDuration;
    private readonly Gauge<int> _activeCashiers;

    public BusinessMetrics(IMeterProvider meterProvider)
    {
        var meter = meterProvider.GetMeter("AppDomain.Business");

        _cashiersCreated = meter.CreateCounter<int>(
            "cashiers_created_total",
            "Total number of cashiers created");

        _commandDuration = meter.CreateHistogram<double>(
            "command_duration_seconds",
            "Duration of command execution");

        _activeCashiers = meter.CreateGauge<int>(
            "active_cashiers",
            "Number of active cashiers");
    }

    public void IncrementCashiersCreated(Guid tenantId)
    {
        _cashiersCreated.Add(1, KeyValuePair.Create("tenant_id", (object)tenantId.ToString()));
    }

    public void RecordCommandDuration(string commandName, TimeSpan duration)
    {
        _commandDuration.Record(duration.TotalSeconds,
            KeyValuePair.Create("command", (object)commandName));
    }
}
```

## Deployment and Operations

### Configuration Management

#### Follow the Configuration Hierarchy Strategy

Momentum applications use a specific configuration strategy designed for cloud-native deployments:

**Environment-Specific Configuration Files:**
- `appsettings.json` contains **baseline configuration** and local development defaults
- `appsettings.{Environment}.json` files contain **environment-specific configuration** for each target environment (Production, QA, Staging)
- `appsettings.Development.json` is **only for local development overrides** and excluded from cloud deployments
- Environment variables are used for **deployment-specific overrides** and values that vary by deployment instance
- **Cloud secret management** (Azure Key Vault, AWS Secrets Manager, etc.) for sensitive data

```json
// appsettings.json - Baseline configuration
{
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "AppDomainDb": "Host=localhost;Port=54320;Database=app_domain;",
    "ServiceBus": "Host=localhost;Port=54320;Database=service_bus;",
    "Messaging": "localhost:9092"
  },
  "Aspire": {
    "Npgsql:DisableHealthChecks": true,
    "Npgsql:DisableTracing": true
  }
}

// appsettings.Production.json - Production environment configuration
{
  "ConnectionStrings": {
    "AppDomainDb": "Host=prod-db;Port=5432;Database=app_domain;",
    "Messaging": "prod-kafka:9092"
  },
  "Aspire": {
    "Npgsql:DisableHealthChecks": false,
    "Npgsql:DisableTracing": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}

// appsettings.Development.json - Local development overrides only (excluded in containers)
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

#### Use Environment Variables for Deployment-Specific Overrides

Use environment variables for values that need to be overridden for specific deployment instances:

```bash
# Deployment-specific overrides via environment variables
export Database__CommandTimeout=45  # Override timeout for this specific deployment
export Logging__LogLevel__Default=Information  # Temporary logging override
export FEATURE_FLAGS__NewDashboard=true  # Feature flag override
```

#### Secure Secrets with Cloud Providers

Never store secrets in configuration files. Use cloud-native secret management:

```csharp
// Azure Key Vault integration
builder.Configuration.AddAzureKeyVault(
    new Uri("https://vault.vault.azure.net/"),
    new DefaultAzureCredential());

// AWS Secrets Manager integration
builder.Configuration.AddSecretsManager(options =>
{
    options.SecretFilter = entry => entry.Name.StartsWith("/myapp/");
});

// Kubernetes secrets (via environment variables)
// Set in deployment manifests or Helm charts
```

#### Validate Critical Configuration

Validate essential configuration at startup:

```csharp
public static class ConfigurationExtensions
{
    public static DatabaseOptions GetDatabaseOptions(this IConfiguration configuration)
    {
        var options = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(options);

        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new InvalidOperationException(
                "Database connection string is required. " +
                "Set ConnectionStrings__AppDomainDb environment variable.");
        }

        if (options.CommandTimeout <= 0)
        {
            throw new InvalidOperationException(
                "Database command timeout must be positive.");
        }

        return options;
    }
}
```

### Health Checks

#### Create Comprehensive Health Checks

```csharp
public class ApplicationHealthCheck : IHealthCheck
{
    private readonly AppDomainDb _db;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<ApplicationHealthCheck> _logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var checks = new List<(string Name, Task<HealthCheckResult> Check)>
        {
            ("Database", CheckDatabaseAsync(cancellationToken)),
            ("Messaging", CheckMessagingAsync(cancellationToken)),
            ("External Dependencies", CheckExternalDependenciesAsync(cancellationToken))
        };

        try
        {
            var results = await Task.WhenAll(checks.Select(c => c.Check));

            var healthData = checks.Zip(results, (check, result) => new
            {
                Name = check.Name,
                Status = result.Status.ToString(),
                Description = result.Description
            }).ToDictionary(x => x.Name, x => (object)new { x.Status, x.Description });

            var unhealthyResults = results.Where(r => r.Status == HealthStatus.Unhealthy).ToList();
            var degradedResults = results.Where(r => r.Status == HealthStatus.Degraded).ToList();

            if (unhealthyResults.Any())
            {
                var errors = string.Join("; ", unhealthyResults.Select(r => r.Description));
                return HealthCheckResult.Unhealthy($"Critical issues: {errors}", null, healthData);
            }

            if (degradedResults.Any())
            {
                var warnings = string.Join("; ", degradedResults.Select(r => r.Description));
                return HealthCheckResult.Degraded($"Performance issues: {warnings}", null, healthData);
            }

            return HealthCheckResult.Healthy("All systems operational", healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy("Health check execution failed", ex);
        }
    }
}
```

## Summary

Following these best practices will help you build robust, scalable, and maintainable applications with Momentum:

1. **Design First**: Think about your domain, boundaries, and event flows before coding
2. **Separate Concerns**: Keep business logic, data access, and infrastructure separate
3. **Optimize Queries**: Use projections, pagination, and appropriate indexing
4. **Handle Errors Gracefully**: Use consistent error handling patterns
5. **Test Thoroughly**: Unit test business logic, integration test workflows
6. **Monitor Everything**: Add logging, metrics, and tracing from the start
7. **Plan for Scale**: Design with performance and resilience in mind
8. **Secure by Default**: Implement security at every layer

Remember: these are guidelines, not absolute rules. Adapt them to your specific context and requirements.

## Next Steps

-   Review the [Troubleshooting Guide](./troubleshooting) for common issues
-   Explore [Testing Strategies](./testing/) in detail
-   Check [Service Configuration](./service-configuration/) for operational setup
-   See the [Architecture Overview](./arch/) for system design patterns
