---
title: Handlers in Momentum
description: Master Momentum's handler architecture with business logic patterns, dependency injection, error handling, and testing strategies.
date: 2024-01-15
---

# Handlers in Momentum

Handlers are the **core execution units** in Momentum's CQRS implementation. They contain your business logic and orchestrate operations between different application layers, providing a clean separation between business rules and infrastructure concerns.

> **Prerequisites**: Understanding of [Commands](./commands) and [Queries](./queries). New to CQRS? Start with our [Getting Started Guide](../getting-started).

## Handler Architecture Overview

Momentum's handler architecture provides clear separation of concerns and promotes testability through structured execution flow:

```mermaid
graph TD
    A["API Request"] -/-> B["Command/Query"]
    B -/-> C["FluentValidation"]
    C -/-> D["Main Handler"]
    D -/-> E["DbCommand Handler"]
    E -/-> F["Database"]
    D -/-> G["Integration Events"]
    G -/-> H["Message Bus (Kafka)"]

    style A fill:#e1f5fe
    style D fill:#f3e5f5
    style E fill:#e8f5e8
    style G fill:#fff3e0
```

### Architecture Benefits

| Layer                 | Responsibility               | Benefits                                     |
| --------------------- | ---------------------------- | -------------------------------------------- |
| **API**               | HTTP request handling        | Clean REST/gRPC interfaces                   |
| **Validation**        | Input validation             | Early error detection, consistent validation |
| **Main Handler**      | Business logic orchestration | Testable business rules                      |
| **DbCommand Handler** | Data access operations       | Optimized database operations                |
| **Events**            | Cross-service communication  | Loose coupling, event-driven architecture    |

### Execution Flow

1. **Request Processing**: API layer receives and routes requests
2. **Validation**: FluentValidation runs automatically before handler execution
3. **Business Logic**: Main handler executes business rules and orchestration
4. **Data Operations**: DbCommand handler performs database operations
5. **Event Publishing**: Integration events are published automatically
6. **Response**: Results are returned through the Result\<T\> pattern

## Handler Types and Patterns

### Command Handlers (Write Operations)

Command handlers orchestrate **state-changing operations** with the two-tier pattern:

```csharp
public static class CreateCashierCommandHandler
{
    // Database command definition
    public record DbCommand(Data.Entities.Cashier Cashier) : ICommand<Data.Entities.Cashier>;

    // Main handler - orchestrates the operation
    public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
        CreateCashierCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // Create database command
        var dbCommand = CreateInsertCommand(command);

        // Execute database operation
        var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        // Transform to domain model
        var result = insertedCashier.ToModel();

        // Create integration event
        var createdEvent = new CashierCreated(result.TenantId, PartitionKeyTest: 0, result);

        return (result, createdEvent);
    }

    // Database handler - performs the actual data operation
    public static async Task<Data.Entities.Cashier> Handle(
        DbCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        return await db.Cashiers.InsertWithOutputAsync(command.Cashier, token: cancellationToken);
    }

    // Helper method to create database command
    private static DbCommand CreateInsertCommand(CreateCashierCommand command) =>
        new(new Data.Entities.Cashier
        {
            TenantId = command.TenantId,
            CashierId = Guid.CreateVersion7(),
            Name = command.Name,
            Email = command.Email,
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        });
}
```

### Query Handlers (Read Operations)

Query handlers are **simpler** and typically use direct database access:

```csharp
public static class GetCashierQueryHandler
{
    public static async Task<Result<Cashier>> Handle(
        GetCashierQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var cashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.TenantId == query.TenantId && c.CashierId == query.Id, cancellationToken);

        if (cashier is not null)
        {
            return cashier.ToModel();
        }

        return new List<ValidationFailure> { new("Id", "Cashier not found") };
    }
}
```

## Handler Discovery and Registration

Momentum automatically discovers and registers handlers through the `DomainAssembly` attribute system:

```csharp
// In your API project's Program.cs or AssemblyInfo.cs
using AppDomain;

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]
```

This enables:

-   Automatic handler discovery
-   Dependency injection registration
-   Validation integration
-   Message routing

## Handler Execution Flow

### Command Execution Flow

1. **Request arrives** at API endpoint
2. **Command validation** runs (FluentValidation)
3. **Main handler executes** with business logic
4. **Database handler executes** for data operations
5. **Integration event published** (if returned)
6. **Result returned** to caller

### Query Execution Flow

1. **Request arrives** at API endpoint
2. **Query validation** runs (if configured)
3. **Query handler executes** with database access
4. **Result returned** to caller

## Advanced Handler Patterns

### Complex Business Logic Handler

This example demonstrates advanced patterns including validation, external service integration, and error handling:

```csharp
public static class UpdateInvoiceCommandHandler
{
    public record DbCommand(Data.Entities.Invoice Invoice) : ICommand<Data.Entities.Invoice>;

    public static async Task<(Result<Invoice>, InvoiceUpdated?)> Handle(
        UpdateInvoiceCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // 1. Fetch existing invoice
        var getQuery = new GetInvoiceQuery(command.TenantId, command.Id);
        var existingResult = await messaging.InvokeAsync(getQuery, cancellationToken);

        if (!existingResult.IsSuccess)
        {
            return (existingResult, null);
        }

        var existing = existingResult.Value;

        // 2. Business rule validation
        if (existing.Status == InvoiceStatus.Paid)
        {
            return (Result<Invoice>.Failure("Cannot update paid invoice"), null);
        }

        // 3. Check if cashier exists (if changing cashier)
        if (command.CashierId != existing.CashierId)
        {
            var cashierQuery = new GetCashierQuery(command.TenantId, command.CashierId);
            var cashierResult = await messaging.InvokeAsync(cashierQuery, cancellationToken);

            if (!cashierResult.IsSuccess)
            {
                return (Result<Invoice>.Failure("Invalid cashier"), null);
            }
        }

        // 4. Create and execute database command
        var dbCommand = CreateUpdateCommand(command, existing);
        var updatedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        // 5. Create integration event
        var result = updatedInvoice.ToModel();
        var updatedEvent = new InvoiceUpdated(result.TenantId, result);

        return (result, updatedEvent);
    }

    public static async Task<Data.Entities.Invoice> Handle(
        DbCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        return await db.Invoices
            .Where(i => i.InvoiceId == command.Invoice.InvoiceId)
            .UpdateWithOutputAsync(
                _ => new Data.Entities.Invoice
                {
                    Amount = command.Invoice.Amount,
                    Description = command.Invoice.Description,
                    CashierId = command.Invoice.CashierId,
                    UpdatedDateUtc = DateTime.UtcNow
                },
                token: cancellationToken);
    }

    private static DbCommand CreateUpdateCommand(UpdateInvoiceCommand command, Invoice existing) =>
        new(new Data.Entities.Invoice
        {
            InvoiceId = existing.Id,
            TenantId = existing.TenantId,
            Amount = command.Amount,
            Description = command.Description,
            CashierId = command.CashierId,
            Status = existing.Status,
            CreatedDateUtc = existing.CreatedDate,
            UpdatedDateUtc = DateTime.UtcNow
        });
}
```

### Handler with External Service Integration

```csharp
public static class ProcessPaymentCommandHandler
{
    public record DbCommand(Guid InvoiceId, InvoiceStatus Status, DateTime PaidDate) : ICommand<Data.Entities.Invoice>;

    public static async Task<(Result<Invoice>, InvoicePaymentProcessed?)> Handle(
        ProcessPaymentCommand command,
        IMessageBus messaging,
        IPaymentService paymentService,
        ILogger<ProcessPaymentCommandHandler> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get invoice
            var getQuery = new GetInvoiceQuery(command.TenantId, command.InvoiceId);
            var invoiceResult = await messaging.InvokeAsync(getQuery, cancellationToken);

            if (!invoiceResult.IsSuccess)
            {
                return (invoiceResult, null);
            }

            var invoice = invoiceResult.Value;

            // 2. Business rules
            if (invoice.Status == InvoiceStatus.Paid)
            {
                return (Result<Invoice>.Failure("Invoice is already paid"), null);
            }

            // 3. Process payment with external service
            logger.LogInformation("Processing payment for invoice {InvoiceId}", command.InvoiceId);

            var paymentResult = await paymentService.ProcessPaymentAsync(
                invoice.Id,
                invoice.Amount,
                command.PaymentDetails,
                cancellationToken);

            if (!paymentResult.IsSuccessful)
            {
                logger.LogWarning("Payment failed for invoice {InvoiceId}: {Error}",
                    command.InvoiceId, paymentResult.ErrorMessage);

                return (Result<Invoice>.Failure($"Payment failed: {paymentResult.ErrorMessage}"), null);
            }

            // 4. Update database
            var dbCommand = new DbCommand(invoice.Id, InvoiceStatus.Paid, DateTime.UtcNow);
            var updatedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

            // 5. Create integration event
            var result = updatedInvoice.ToModel();
            var paymentProcessedEvent = new InvoicePaymentProcessed(
                result.TenantId,
                result.Id,
                paymentResult.TransactionId);

            logger.LogInformation("Payment processed successfully for invoice {InvoiceId}", command.InvoiceId);

            return (result, paymentProcessedEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing payment for invoice {InvoiceId}", command.InvoiceId);
            return (Result<Invoice>.Failure("Payment processing failed due to system error"), null);
        }
    }

    public static async Task<Data.Entities.Invoice> Handle(
        DbCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        return await db.Invoices
            .Where(i => i.InvoiceId == command.InvoiceId)
            .UpdateWithOutputAsync(
                _ => new Data.Entities.Invoice
                {
                    Status = command.Status,
                    PaidDateUtc = command.PaidDate,
                    UpdatedDateUtc = DateTime.UtcNow
                },
                token: cancellationToken);
    }
}
```

## Handler Testing

### Unit Testing Main Handlers

```csharp
[Test]
public async Task Handle_ValidCommand_ReturnsSuccessResult()
{
    // Arrange
    var command = new CreateCashierCommand(
        TenantId: Guid.NewGuid(),
        Name: "John Doe",
        Email: "john@example.com"
    );

    var mockMessaging = new Mock<IMessageBus>();
    var expectedEntity = new Data.Entities.Cashier
    {
        CashierId = Guid.NewGuid(),
        Name = command.Name,
        Email = command.Email
    };

    mockMessaging
        .Setup(m => m.InvokeCommandAsync(It.IsAny<CreateCashierCommandHandler.DbCommand>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedEntity);

    // Act
    var (result, integrationEvent) = await CreateCashierCommandHandler.Handle(command, mockMessaging.Object, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Name.Should().Be(command.Name);
    result.Value.Email.Should().Be(command.Email);
    integrationEvent.Should().NotBeNull();
    integrationEvent!.TenantId.Should().Be(command.TenantId);
}
```

### Integration Testing Database Handlers

```csharp
[Test]
public async Task Handle_DbCommand_InsertsAndReturnsEntity()
{
    // Arrange
    using var testContext = new TestContext();
    var db = testContext.CreateDatabase<AppDomainDb>();

    var entity = new Data.Entities.Cashier
    {
        TenantId = Guid.NewGuid(),
        CashierId = Guid.NewGuid(),
        Name = "Jane Doe",
        Email = "jane@example.com",
        CreatedDateUtc = DateTime.UtcNow,
        UpdatedDateUtc = DateTime.UtcNow
    };

    var command = new CreateCashierCommandHandler.DbCommand(entity);

    // Act
    var result = await CreateCashierCommandHandler.Handle(command, db, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.CashierId.Should().Be(entity.CashierId);
    result.Name.Should().Be(entity.Name);
    result.Email.Should().Be(entity.Email);

    // Verify it was actually inserted
    var inserted = await db.Cashiers.FirstOrDefaultAsync(c => c.CashierId == entity.CashierId);
    inserted.Should().NotBeNull();
}
```

## Error Handling in Handlers

### Graceful Error Handling

```csharp
public static async Task<(Result<Invoice>, InvoiceCreated?)> Handle(
    CreateInvoiceCommand command,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    try
    {
        // Verify cashier exists
        var cashierQuery = new GetCashierQuery(command.TenantId, command.CashierId);
        var cashierResult = await messaging.InvokeAsync(cashierQuery, cancellationToken);

        if (!cashierResult.IsSuccess)
        {
            return (Result<Invoice>.Failure("Invalid cashier specified"), null);
        }

        // Business rule validation
        if (command.Amount <= 0)
        {
            return (Result<Invoice>.Failure("Invoice amount must be greater than zero"), null);
        }

        // Execute database operation
        var dbCommand = CreateInsertCommand(command);
        var insertedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedInvoice.ToModel();
        var createdEvent = new InvoiceCreated(result.TenantId, result);

        return (result, createdEvent);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        // Log the exception but don't expose internal details
        return (Result<Invoice>.Failure("An error occurred while creating the invoice"), null);
    }
}
```

### Database Error Handling

```csharp
public static async Task<Data.Entities.Cashier> Handle(
    DbCommand command,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    try
    {
        return await db.Cashiers.InsertWithOutputAsync(command.Cashier, token: cancellationToken);
    }
    catch (SqlException ex) when (ex.Number == 2627) // Unique constraint violation
    {
        throw new InvalidOperationException("A cashier with this email already exists");
    }
    catch (SqlException ex) when (ex.Number == 547) // Foreign key violation
    {
        throw new InvalidOperationException("Invalid tenant specified");
    }
}
```

## Dependency Injection in Handlers

### Service Dependencies

Handlers receive dependencies through **method parameters** (not constructor injection):

```csharp
public static async Task<(Result<User>, UserCreated?)> Handle(
    CreateUserCommand command,
    IMessageBus messaging,
    IEmailService emailService,
    IUserValidationService validationService,
    ILogger<CreateUserCommandHandler> logger,
    CancellationToken cancellationToken)
{
    // Use injected services
    var isValid = await validationService.ValidateUserAsync(command.Email, cancellationToken);
    if (!isValid)
    {
        return (Result<User>.Failure("Invalid user data"), null);
    }

    // ... handler logic ...

    // Send welcome email
    await emailService.SendWelcomeEmailAsync(result.Value.Email, cancellationToken);

    logger.LogInformation("User created: {UserId}", result.Value.Id);

    return (result, createdEvent);
}
```

## Handler Best Practices

### Design Principles

#### ✅ Single Responsibility Principle

```csharp
// ✅ Good: Handler does one thing well
public static class CreateCashierCommandHandler
{
    public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
        CreateCashierCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // Single responsibility: Create a cashier
        var dbCommand = CreateInsertCommand(command);
        var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedCashier.ToModel();
        var createdEvent = new CashierCreated(result.TenantId, result);

        return (result, createdEvent);
    }
}

// ❌ Bad: Handler trying to do too much
public static class CreateCashierAndSetupAccountCommandHandler
{
    public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
        CreateCashierAndSetupAccountCommand command,
        IMessageBus messaging,
        IEmailService emailService,
        IPayrollService payrollService,
        CancellationToken cancellationToken)
    {
        // Too many responsibilities:
        // 1. Create cashier
        // 2. Send welcome email
        // 3. Setup payroll
        // 4. Configure permissions
        // Should be separate handlers/commands
    }
}
```

#### ✅ Fail Fast Pattern

```csharp
public static async Task<(Result<Invoice>, InvoiceUpdated?)> Handle(
    UpdateInvoiceCommand command,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // ✅ Validate early, fail fast
    if (command.Amount <= 0)
    {
        return (Result<Invoice>.Failure("Invoice amount must be greater than zero"), null);
    }

    // Check if invoice exists
    var getQuery = new GetInvoiceQuery(command.TenantId, command.InvoiceId);
    var existingResult = await messaging.InvokeAsync(getQuery, cancellationToken);

    if (!existingResult.IsSuccess)
    {
        return (existingResult, null); // Fail fast on missing invoice
    }

    var existing = existingResult.Value;

    // Business rule validation
    if (existing.Status == InvoiceStatus.Paid)
    {
        return (Result<Invoice>.Failure("Cannot update paid invoice"), null);
    }

    // Continue with update...
}
```

### Performance Guidelines

#### Asynchronous Operations

```csharp
// ✅ Proper async/await usage
public static async Task<Result<List<Cashier>>> Handle(
    GetCashiersQuery query,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    var cashiers = await db.Cashiers
        .Where(c => c.TenantId == query.TenantId)
        .OrderBy(c => c.Name)
        .ToListAsync(cancellationToken); // Pass cancellation token

    return cashiers.Select(c => c.ToModel()).ToList();
}

// ❌ Bad: Blocking async operations
public static Result<List<Cashier>> Handle(
    GetCashiersQuery query,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    // Don't block async operations
    var cashiers = db.Cashiers
        .Where(c => c.TenantId == query.TenantId)
        .ToListAsync(cancellationToken)
        .Result; // ❌ Blocking call - can cause deadlocks

    return cashiers.Select(c => c.ToModel()).ToList();
}
```

#### Database Query Optimization

```csharp
// ✅ Optimized query with projection
public static async Task<Result<PagedResult<CashierSummary>>> Handle(
    GetCashierSummariesQuery query,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    var skip = (query.Page - 1) * query.PageSize;

    // Use projection to minimize data transfer
    var cashierQuery = db.Cashiers
        .Where(c => c.TenantId == query.TenantId)
        .Select(c => new CashierSummary
        {
            Id = c.CashierId,
            Name = c.Name,
            Email = c.Email,
            IsActive = c.IsActive
            // Only select needed fields
        });

    var totalCount = await cashierQuery.CountAsync(cancellationToken);
    var items = await cashierQuery
        .OrderBy(c => c.Name)
        .Skip(skip)
        .Take(query.PageSize)
        .ToListAsync(cancellationToken);

    return new PagedResult<CashierSummary>
    {
        Items = items,
        TotalCount = totalCount,
        Page = query.Page,
        PageSize = query.PageSize
    };
}
```

### Error Handling Patterns

#### Result Pattern Implementation

```csharp
// ✅ Consistent error handling with Result pattern
public static async Task<Result<Cashier>> Handle(
    GetCashierQuery query,
    AppDomainDb db,
    ILogger<GetCashierQueryHandler> logger,
    CancellationToken cancellationToken)
{
    try
    {
        var cashier = await db.Cashiers
            .FirstOrDefaultAsync(c =>
                c.TenantId == query.TenantId &&
                c.CashierId == query.Id,
                cancellationToken);

        if (cashier != null)
        {
            return cashier.ToModel(); // Success case
        }

        // Business failure (not exceptional)
        return new List<ValidationFailure> { new("Id", "Cashier not found") };
    }
    catch (OperationCanceledException)
    {
        // Don't log cancellation as error
        throw;
    }
    catch (Exception ex)
    {
        // Log infrastructure failures
        logger.LogError(ex, "Database error retrieving cashier {CashierId} for tenant {TenantId}",
            query.Id, query.TenantId);

        // Return user-friendly error
        return Result<Cashier>.Failure("An error occurred while retrieving the cashier");
    }
}
```

### Logging Best Practices

```csharp
// ✅ Structured logging with context
public static async Task<(Result<Invoice>, InvoiceCreated?)> Handle(
    CreateInvoiceCommand command,
    IMessageBus messaging,
    ILogger<CreateInvoiceCommandHandler> logger,
    CancellationToken cancellationToken)
{
    using var scope = logger.BeginScope(new Dictionary<string, object>
    {
        ["TenantId"] = command.TenantId,
        ["CashierId"] = command.CashierId,
        ["Amount"] = command.Amount
    });

    logger.LogInformation("Creating invoice for cashier {CashierId} with amount {Amount}",
        command.CashierId, command.Amount);

    try
    {
        var dbCommand = CreateInsertCommand(command);
        var insertedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedInvoice.ToModel();
        var createdEvent = new InvoiceCreated(result.TenantId, result);

        logger.LogInformation("Successfully created invoice {InvoiceId}", result.Id);

        return (result, createdEvent);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create invoice");
        throw;
    }
}
```

## Next Steps

Now that you understand handlers, continue with these related topics:

### Core CQRS Concepts

1. **[Commands](./commands)** - Write operations and state modification patterns
2. **[Queries](./queries)** - Read operations and data retrieval optimization
3. **[Validation](./validation)** - FluentValidation integration and error handling

### Advanced Implementation

4. **[Database Integration](../database/dbcommand)** - DbCommand pattern and data access
5. **[Error Handling](../error-handling)** - Result pattern and exception management
6. **[Messaging](../messaging/)** - Integration events and cross-service communication

### Testing and Quality

7. **[Testing Handlers](../testing/unit-tests#testing-handlers)** - Unit and integration testing strategies
8. **[Best Practices](../best-practices#handler-patterns)** - Production-ready handler patterns
9. **[Troubleshooting](../troubleshooting#handler-issues)** - Common handler issues and solutions

### Performance and Observability

10. **[Service Configuration](../service-configuration/)** - Logging, metrics, and monitoring setup
11. **[Performance Optimization](../best-practices#performance-optimization)** - Handler performance tuning
