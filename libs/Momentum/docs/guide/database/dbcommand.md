---
title: DbCommand Pattern
description: Type-safe, testable approach to database operations with separation of concerns and performance optimization.
date: 2024-01-15
---

# DbCommand Pattern in Momentum

The DbCommand pattern in Momentum provides a type-safe, testable approach to database operations. It separates database logic from business logic while maintaining consistency and performance.

## What is DbCommand?

The DbCommand pattern:

-   **Separates concerns**: Database operations are isolated from business logic
-   **Improves testability**: Business logic can be unit tested without database
-   **Ensures consistency**: All database operations follow the same pattern
-   **Provides type safety**: Compile-time checking of database operations

## Basic DbCommand Structure

### Command Handler with DbCommand

```csharp
public static class CreateCashierCommandHandler
{
    // Database command definition
    public record DbCommand(Data.Entities.Cashier Cashier) : ICommand<Data.Entities.Cashier>;

    // Main handler - orchestrates business logic
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

    // Database handler - performs actual database operation
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

## DbCommand Patterns

### Insert Operations

```csharp
public static class CreateInvoiceCommandHandler
{
    public record DbCommand(Data.Entities.Invoice Invoice) : ICommand<Data.Entities.Invoice>;

    public static async Task<(Result<Invoice>, InvoiceCreated?)> Handle(
        CreateInvoiceCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = CreateInsertCommand(command);
        var insertedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedInvoice.ToModel();
        var createdEvent = new InvoiceCreated(result.TenantId, result);

        return (result, createdEvent);
    }

    public static async Task<Data.Entities.Invoice> Handle(
        DbCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        return await db.Invoices.InsertWithOutputAsync(command.Invoice, token: cancellationToken);
    }

    private static DbCommand CreateInsertCommand(CreateInvoiceCommand command) =>
        new(new Data.Entities.Invoice
        {
            TenantId = command.TenantId,
            InvoiceId = Guid.CreateVersion7(),
            Amount = command.Amount,
            Description = command.Description,
            CashierId = command.CashierId,
            Status = InvoiceStatus.Draft,
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        });
}
```

### Update Operations

```csharp
public static class UpdateCashierCommandHandler
{
    public record DbCommand(
        Guid TenantId,
        Guid CashierId,
        string Name,
        string? Email,
        int Version
    ) : ICommand<Data.Entities.Cashier?>;

    public static async Task<(Result<Cashier>, CashierUpdated?)> Handle(
        UpdateCashierCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // First, verify the cashier exists
        var getQuery = new GetCashierQuery(command.TenantId, command.CashierId);
        var existingResult = await messaging.InvokeAsync(getQuery, cancellationToken);

        if (!existingResult.IsSuccess)
        {
            return (existingResult, null);
        }

        var existing = existingResult.Value;

        // Create update command
        var dbCommand = new DbCommand(
            command.TenantId,
            command.CashierId,
            command.Name,
            command.Email,
            existing.Version
        );

        var updatedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (updatedCashier is null)
        {
            return (Result<Cashier>.Failure("Cashier was modified by another process"), null);
        }

        var result = updatedCashier.ToModel();
        var updatedEvent = new CashierUpdated(result.TenantId, result.Id);

        return (result, updatedEvent);
    }

    public static async Task<Data.Entities.Cashier?> Handle(
        DbCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        return await db.Cashiers
            .Where(c => c.TenantId == command.TenantId &&
                       c.CashierId == command.CashierId &&
                       c.Version == command.Version)
            .UpdateWithOutputAsync(
                _ => new Data.Entities.Cashier
                {
                    Name = command.Name,
                    Email = command.Email,
                    UpdatedDateUtc = DateTime.UtcNow
                },
                token: cancellationToken);
    }
}
```

### Delete Operations

```csharp
public static class DeleteCashierCommandHandler
{
    public record DbCommand(Guid TenantId, Guid CashierId) : ICommand<int>;

    public static async Task<(Result<bool>, CashierDeleted?)> Handle(
        DeleteCashierCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // Verify the cashier exists first
        var getQuery = new GetCashierQuery(command.TenantId, command.CashierId);
        var existingResult = await messaging.InvokeAsync(getQuery, cancellationToken);

        if (!existingResult.IsSuccess)
        {
            return (existingResult.Errors, null);
        }

        // Execute delete
        var dbCommand = new DbCommand(command.TenantId, command.CashierId);
        var deletedCount = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (deletedCount > 0)
        {
            var deletedEvent = new CashierDeleted(command.TenantId, command.CashierId);
            return (true, deletedEvent);
        }

        return (Result<bool>.Failure("Cashier could not be deleted"), null);
    }

    public static async Task<int> Handle(
        DbCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        return await db.Cashiers
            .Where(c => c.TenantId == command.TenantId && c.CashierId == command.CashierId)
            .DeleteAsync(token: cancellationToken);
    }
}
```

## Source-Generated DbCommands

Momentum supports source-generated database commands for enhanced performance and type safety:

### Query with Database Function

```csharp
public static partial class GetCashiersQueryHandler
{
    /// <summary>
    /// If the function name starts with a $, the function gets executed as `select * from {dbFunction}`
    /// </summary>
    [DbCommand(fn: "$AppDomain.cashiers_get_all")]
    public partial record DbQuery(
        Guid TenantId,
        int Limit,
        int Offset
    ) : IQuery<IEnumerable<Data.Entities.Cashier>>;

    public static async Task<IEnumerable<GetCashiersQuery.Result>> Handle(
        GetCashiersQuery query,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbQuery = new DbQuery(query.TenantId, query.Limit, query.Offset);
        var cashiers = await messaging.InvokeQueryAsync(dbQuery, cancellationToken);

        return cashiers.Select(c => new GetCashiersQuery.Result(
            c.TenantId,
            c.CashierId,
            c.Name,
            c.Email ?? "N/A"
        ));
    }
}
```

### Source Generator Benefits

1. **Performance**: Eliminates reflection and runtime compilation
2. **Type Safety**: Compile-time validation of SQL and parameters
3. **IntelliSense**: Full IDE support with parameter completion
4. **Debugging**: Generated code is debuggable

### Database Function Example

The corresponding PostgreSQL function:

```sql
-- SQL function: AppDomain.cashiers_get_all
CREATE OR REPLACE FUNCTION "AppDomain".cashiers_get_all(
    p_tenant_id UUID,
    p_limit INTEGER,
    p_offset INTEGER
)
RETURNS TABLE (
    tenant_id UUID,
    cashier_id UUID,
    name VARCHAR,
    email VARCHAR,
    created_date_utc TIMESTAMP,
    updated_date_utc TIMESTAMP,
    version INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        c.tenant_id,
        c.cashier_id,
        c.name,
        c.email,
        c.created_date_utc,
        c.updated_date_utc,
        c.version
    FROM "AppDomain".cashiers c
    WHERE c.tenant_id = p_tenant_id
    ORDER BY c.name
    LIMIT p_limit
    OFFSET p_offset;
END;
$$;
```

## Advanced DbCommand Patterns

### Bulk Operations for High Performance

```csharp
public static class BulkInsertCashiersCommandHandler
{
    public record DbCommand(List<Data.Entities.Cashier> Cashiers) : ICommand<int>;

    public static async Task<(Result<int>, CashiersBulkInserted?)> Handle(
        BulkInsertCashiersCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var entities = command.Cashiers.Select(c => new Data.Entities.Cashier
        {
            TenantId = c.TenantId,
            CashierId = Guid.CreateVersion7(),
            Name = c.Name,
            Email = c.Email,
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        }).ToList();

        var dbCommand = new DbCommand(entities);
        var insertedCount = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var bulkInsertedEvent = new CashiersBulkInserted(command.TenantId, insertedCount);
        return (insertedCount, bulkInsertedEvent);
    }

    public static async Task<int> Handle(
        DbCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        return await db.BulkCopyAsync(command.Cashiers, cancellationToken);
    }
}
```

### Complex Queries and Joins

```csharp
public static class GetCashierWithInvoicesCommandHandler
{
    public record DbCommand(Guid TenantId, Guid CashierId) : IQuery<CashierWithInvoicesResult>;

    public record CashierWithInvoicesResult(
        Data.Entities.Cashier Cashier,
        List<Data.Entities.Invoice> Invoices
    );

    public static async Task<Result<CashierWithInvoices>> Handle(
        GetCashierWithInvoicesQuery query,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = new DbCommand(query.TenantId, query.CashierId);
        var result = await messaging.InvokeQueryAsync(dbCommand, cancellationToken);

        if (result.Cashier == null)
        {
            return Result<CashierWithInvoices>.Failure("Cashier not found");
        }

        return new CashierWithInvoices
        {
            Cashier = result.Cashier.ToModel(),
            Invoices = result.Invoices.Select(i => i.ToModel()).ToList()
        };
    }

    public static async Task<CashierWithInvoicesResult> Handle(
        DbCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var cashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.TenantId == command.TenantId &&
                                     c.CashierId == command.CashierId,
                                cancellationToken);

        if (cashier == null)
        {
            return new CashierWithInvoicesResult(null, new List<Data.Entities.Invoice>());
        }

        var invoices = await db.Invoices
            .Where(i => i.TenantId == command.TenantId && i.CashierId == command.CashierId)
            .ToListAsync(cancellationToken);

        return new CashierWithInvoicesResult(cashier, invoices);
    }
}
```

### Transaction Management

```csharp
public static class TransferInvoiceCommandHandler
{
    public record DbCommand(
        Guid TenantId,
        Guid InvoiceId,
        Guid FromCashierId,
        Guid ToCashierId
    ) : ICommand<bool>;

    public static async Task<(Result<bool>, InvoiceTransferred?)> Handle(
        TransferInvoiceCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = new DbCommand(
            command.TenantId,
            command.InvoiceId,
            command.FromCashierId,
            command.ToCashierId
        );

        var success = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (success)
        {
            var transferredEvent = new InvoiceTransferred(
                command.TenantId,
                command.InvoiceId,
                command.FromCashierId,
                command.ToCashierId
            );

            return (true, transferredEvent);
        }

        return (Result<bool>.Failure("Transfer failed"), null);
    }

    public static async Task<bool> Handle(
        DbCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        // Transaction is automatically handled by Wolverine
        using var transaction = await db.BeginTransactionAsync(cancellationToken);

        try
        {
            // Verify source cashier has the invoice
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(i => i.TenantId == command.TenantId &&
                                         i.InvoiceId == command.InvoiceId &&
                                         i.CashierId == command.FromCashierId,
                                    cancellationToken);

            if (invoice == null)
            {
                return false;
            }

            // Verify target cashier exists
            var targetCashier = await db.Cashiers
                .AnyAsync(c => c.TenantId == command.TenantId &&
                              c.CashierId == command.ToCashierId,
                         cancellationToken);

            if (!targetCashier)
            {
                return false;
            }

            // Transfer the invoice
            var updateCount = await db.Invoices
                .Where(i => i.InvoiceId == command.InvoiceId)
                .UpdateAsync(_ => new Data.Entities.Invoice
                {
                    CashierId = command.ToCashierId,
                    UpdatedDateUtc = DateTime.UtcNow
                }, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return updateCount > 0;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

## Testing DbCommands

### Unit Testing Main Handlers

```csharp
[Test]
public async Task Handle_ValidCommand_CreatesDbCommandAndProcessesResult()
{
    // Arrange
    var command = new CreateCashierCommand(
        Guid.NewGuid(),
        "John Doe",
        "john@example.com"
    );

    var expectedEntity = new Data.Entities.Cashier
    {
        TenantId = command.TenantId,
        CashierId = Guid.NewGuid(),
        Name = command.Name,
        Email = command.Email,
        CreatedDateUtc = DateTime.UtcNow,
        UpdatedDateUtc = DateTime.UtcNow
    };

    var mockMessaging = new Mock<IMessageBus>();
    mockMessaging
        .Setup(m => m.InvokeCommandAsync(
            It.IsAny<CreateCashierCommandHandler.DbCommand>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedEntity);

    // Act
    var (result, integrationEvent) = await CreateCashierCommandHandler.Handle(
        command, mockMessaging.Object, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Name.Should().Be(command.Name);
    result.Value.Email.Should().Be(command.Email);

    integrationEvent.Should().NotBeNull();
    integrationEvent!.TenantId.Should().Be(command.TenantId);

    // Verify DbCommand was called
    mockMessaging.Verify(
        m => m.InvokeCommandAsync(
            It.Is<CreateCashierCommandHandler.DbCommand>(dc =>
                dc.Cashier.Name == command.Name &&
                dc.Cashier.Email == command.Email),
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### Integration Testing Database Handlers

```csharp
[Test]
public async Task Handle_DbCommand_InsertsEntityAndReturnsResult()
{
    // Arrange
    using var testContext = new IntegrationTestContext();
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

    var dbCommand = new CreateCashierCommandHandler.DbCommand(entity);

    // Act
    var result = await CreateCashierCommandHandler.Handle(dbCommand, db, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.CashierId.Should().Be(entity.CashierId);
    result.Name.Should().Be(entity.Name);
    result.Email.Should().Be(entity.Email);

    // Verify database state
    var inserted = await db.Cashiers
        .FirstOrDefaultAsync(c => c.CashierId == entity.CashierId);

    inserted.Should().NotBeNull();
    inserted!.Name.Should().Be(entity.Name);
    inserted.Email.Should().Be(entity.Email);
}
```

### Testing Source-Generated Commands

```csharp
[Test]
public async Task Handle_SourceGeneratedDbQuery_ReturnsResults()
{
    // Arrange
    using var testContext = new IntegrationTestContext();
    var messaging = testContext.GetService<IMessageBus>();

    var tenantId = Guid.NewGuid();

    // Insert test data
    var testCashiers = new[]
    {
        new CreateCashierCommand(tenantId, "Alice", "alice@test.com"),
        new CreateCashierCommand(tenantId, "Bob", "bob@test.com")
    };

    foreach (var cashierCommand in testCashiers)
    {
        await messaging.InvokeAsync(cashierCommand);
    }

    var query = new GetCashiersQuery(tenantId, Offset: 0, Limit: 10);

    // Act
    var result = await messaging.InvokeAsync(query);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(2);
    result.Should().Contain(r => r.Name == "Alice");
    result.Should().Contain(r => r.Name == "Bob");
}
```

## Best Practices

### DbCommand Design

1. **Single Responsibility**: Each DbCommand should perform one database operation
2. **Type Safety**: Use strongly-typed parameters and return types
3. **Naming**: Use descriptive names that indicate the operation
4. **Immutability**: DbCommands should be immutable records

### Handler Organization

1. **Co-location**: Keep DbCommand definitions with their handlers
2. **Separation**: Separate business logic from database operations
3. **Consistency**: Follow the same pattern across all handlers
4. **Documentation**: Document complex database operations

### Performance

1. **Async Operations**: Always use async/await for database operations
2. **Cancellation**: Support cancellation tokens in all operations
3. **Bulk Operations**: Use bulk operations for large data sets
4. **Indexing**: Ensure proper database indexing for queries

### Error Handling

1. **Specific Exceptions**: Handle database-specific exceptions appropriately
2. **Transactions**: Use transactions for multi-step operations
3. **Optimistic Concurrency**: Use version fields to handle concurrent updates
4. **Logging**: Log database operations for debugging and monitoring

### Testing

1. **Unit Test Logic**: Test business logic independently from database
2. **Integration Test Database**: Test database operations with real databases
3. **Mock Appropriately**: Mock IMessageBus in unit tests, use real database in integration tests
4. **Test Edge Cases**: Test concurrent access, constraint violations, etc.

## Next Steps

- Learn about [Entity Mapping](./entity-mapping) for database schema design
- Understand [Transactions](./transactions) for complex operations
- Explore [Best Practices](../best-practices) for performance optimization
- See [Testing](../testing/) for comprehensive database testing strategies
