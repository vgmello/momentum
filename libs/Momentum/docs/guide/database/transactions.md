---
title: Database Transactions
description: Robust transaction handling with LinqToDB integration and outbox pattern for reliable event publishing and data consistency.
date: 2024-01-15
---

# Database Transactions in Momentum

Momentum provides robust transaction handling through LinqToDB integration and follows the outbox pattern for reliable event publishing. This ensures data consistency and reliable message delivery in distributed systems.

## Overview

Database transactions in Momentum are handled automatically by the framework, but you can also control them explicitly when needed:

```csharp
public static class CreateCashierCommandHandler
{
    public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
        CreateCashierCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // Database operations are automatically wrapped in transactions
        var dbCommand = CreateInsertCommand(command);
        var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedCashier.ToModel();
        var createdEvent = new CashierCreated(result.TenantId, 0, result);

        // Event will be published in the same transaction
        return (result, createdEvent);
    }
}
```

## Automatic Transaction Management

### Command Handler Transactions

All command handlers automatically run within transactions:

```csharp
public static class UpdateCashierCommandHandler
{
    public record DbCommand(Data.Entities.Cashier Cashier) : ICommand<Data.Entities.Cashier>;

    public static async Task<(Result<Cashier>, CashierUpdated?)> Handle(
        UpdateCashierCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // All operations in this handler run in a single transaction:
        
        // 1. Query existing cashier
        var getQuery = new GetCashierQuery(command.TenantId, command.Id);
        var existingResult = await messaging.InvokeAsync(getQuery, cancellationToken);

        if (!existingResult.IsSuccess)
        {
            return (existingResult, null);
        }

        // 2. Update database
        var dbCommand = CreateUpdateCommand(command, existingResult.Value);
        var updatedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = updatedCashier.ToModel();
        var updatedEvent = new CashierUpdated(result.TenantId, result);

        // 3. Event publishing (handled by framework)
        return (result, updatedEvent);
        
        // If any step fails, the entire transaction is rolled back
    }

    public static async Task<Data.Entities.Cashier> Handle(
        DbCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        // This database operation participates in the parent transaction
        return await db.Cashiers
            .Where(c => c.CashierId == command.Cashier.CashierId)
            .UpdateWithOutputAsync(
                _ => new Data.Entities.Cashier
                {
                    Name = command.Cashier.Name,
                    Email = command.Cashier.Email,
                    UpdatedDateUtc = DateTime.UtcNow
                },
                token: cancellationToken);
    }
}
```

### Transaction Scope

Each command handler execution creates a transaction scope that includes:

1. **Database Operations**: All DbCommand executions
2. **Event Publishing**: Integration events are stored for later publishing
3. **Side Effects**: Any other transactional resources

## Explicit Transaction Control

### Manual Transaction Management

For complex scenarios, you can manage transactions explicitly:

```csharp
public static class ComplexBusinessOperationHandler
{
    public static async Task<Result<BusinessOperationResult>> Handle(
        ComplexBusinessOperationCommand command,
        AppDomainDb db,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        using var transaction = await db.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Step 1: Create primary entity
            var cashier = new Data.Entities.Cashier
            {
                TenantId = command.TenantId,
                CashierId = Guid.CreateVersion7(),
                Name = command.CashierName,
                Email = command.CashierEmail,
                CreatedDateUtc = DateTime.UtcNow,
                UpdatedDateUtc = DateTime.UtcNow
            };

            var insertedCashier = await db.Cashiers.InsertWithOutputAsync(cashier, token: cancellationToken);

            // Step 2: Create related entities
            var permissions = command.Permissions.Select(p => new Data.Entities.CashierPermission
            {
                CashierId = insertedCashier.CashierId,
                Permission = p,
                GrantedDateUtc = DateTime.UtcNow
            }).ToList();

            await db.CashierPermissions.BulkCopyAsync(permissions, cancellationToken);

            // Step 3: Update statistics
            await db.TenantStats
                .Where(ts => ts.TenantId == command.TenantId)
                .UpdateAsync(ts => new Data.Entities.TenantStat
                {
                    CashierCount = ts.CashierCount + 1,
                    UpdatedDateUtc = DateTime.UtcNow
                }, cancellationToken);

            // Step 4: Store events for publishing
            var events = new List<object>
            {
                new CashierCreated(command.TenantId, 0, insertedCashier.ToModel()),
                new CashierPermissionsGranted(command.TenantId, insertedCashier.CashierId, command.Permissions)
            };

            foreach (var evt in events)
            {
                await StoreEventForPublishing(evt, db, cancellationToken);
            }

            // Commit all changes
            await transaction.CommitAsync(cancellationToken);

            // Publish events after successful commit
            foreach (var evt in events)
            {
                await messaging.PublishAsync(evt, cancellationToken);
            }

            return new BusinessOperationResult
            {
                CashierId = insertedCashier.CashierId,
                PermissionsGranted = permissions.Count
            };
        }
        catch (Exception ex)
        {
            // Rollback on any error
            await transaction.RollbackAsync(cancellationToken);
            
            return Result<BusinessOperationResult>.Failure($"Operation failed: {ex.Message}");
        }
    }

    private static async Task StoreEventForPublishing(
        object evt,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var outboxEvent = new Data.Entities.OutboxEvent
        {
            Id = Guid.CreateVersion7(),
            EventType = evt.GetType().Name,
            EventData = JsonSerializer.Serialize(evt),
            CreatedDateUtc = DateTime.UtcNow
        };

        await db.OutboxEvents.InsertAsync(outboxEvent, token: cancellationToken);
    }
}
```

### Nested Transactions

Handle nested operations with savepoints:

```csharp
public static class BatchProcessingHandler
{
    public static async Task<Result<BatchResult>> Handle(
        BatchProcessingCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        using var mainTransaction = await db.BeginTransactionAsync(cancellationToken);
        var results = new List<ProcessingResult>();
        var errors = new List<string>();

        try
        {
            foreach (var item in command.Items)
            {
                // Create savepoint for each item
                var savepoint = $"item_{item.Id}";
                await mainTransaction.SaveAsync(savepoint, cancellationToken);

                try
                {
                    var result = await ProcessSingleItem(item, db, cancellationToken);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    // Rollback to savepoint on item failure
                    await mainTransaction.RollbackAsync(savepoint, cancellationToken);
                    errors.Add($"Item {item.Id}: {ex.Message}");
                    
                    // Continue processing other items
                }
            }

            // Decide whether to commit based on success criteria
            if (errors.Count == 0 || errors.Count < command.Items.Count * 0.5) // Allow up to 50% failures
            {
                await mainTransaction.CommitAsync(cancellationToken);
                
                return new BatchResult
                {
                    Successful = results,
                    Errors = errors,
                    Success = true
                };
            }
            else
            {
                await mainTransaction.RollbackAsync(cancellationToken);
                
                return Result<BatchResult>.Failure($"Too many failures: {errors.Count}/{command.Items.Count}");
            }
        }
        catch (Exception ex)
        {
            await mainTransaction.RollbackAsync(cancellationToken);
            return Result<BatchResult>.Failure($"Batch processing failed: {ex.Message}");
        }
    }
}
```

## Outbox Pattern Implementation

### Event Storage

Momentum implements the outbox pattern to ensure reliable event publishing:

```csharp
// Database entity for storing events
public class OutboxEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime CreatedDateUtc { get; set; }
    public DateTime? PublishedDateUtc { get; set; }
    public bool IsPublished { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
```

### Automatic Event Storage

Events returned from command handlers are automatically stored:

```csharp
// Framework automatically handles this pattern:
public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
    CreateCashierCommand command,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // 1. Execute database operations
    var dbCommand = CreateInsertCommand(command);
    var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

    var result = insertedCashier.ToModel();
    var createdEvent = new CashierCreated(result.TenantId, 0, result);

    // 2. Framework stores event in outbox table (same transaction)
    // 3. Framework publishes event after transaction commit
    // 4. Framework marks event as published

    return (result, createdEvent);
}
```

### Outbox Event Publisher

Background service that publishes stored events:

```csharp
public class OutboxEventPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxEventPublisher> _logger;

    public OutboxEventPublisher(IServiceProvider serviceProvider, ILogger<OutboxEventPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEvents(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task ProcessOutboxEvents(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var unpublishedEvents = await db.OutboxEvents
            .Where(e => !e.IsPublished && e.RetryCount < 5)
            .OrderBy(e => e.CreatedDateUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var outboxEvent in unpublishedEvents)
        {
            try
            {
                await PublishEvent(outboxEvent, messageBus, cancellationToken);
                
                // Mark as published
                await db.OutboxEvents
                    .Where(e => e.Id == outboxEvent.Id)
                    .UpdateAsync(e => new OutboxEvent
                    {
                        IsPublished = true,
                        PublishedDateUtc = DateTime.UtcNow
                    }, cancellationToken);
                    
                _logger.LogDebug("Published outbox event {EventId} of type {EventType}", 
                    outboxEvent.Id, outboxEvent.EventType);
            }
            catch (Exception ex)
            {
                // Increment retry count and log error
                await db.OutboxEvents
                    .Where(e => e.Id == outboxEvent.Id)
                    .UpdateAsync(e => new OutboxEvent
                    {
                        RetryCount = e.RetryCount + 1,
                        LastError = ex.Message
                    }, cancellationToken);
                    
                _logger.LogError(ex, "Failed to publish outbox event {EventId} of type {EventType}", 
                    outboxEvent.Id, outboxEvent.EventType);
            }
        }
    }

    private async Task PublishEvent(OutboxEvent outboxEvent, IMessageBus messageBus, CancellationToken cancellationToken)
    {
        // Deserialize event based on type
        var eventType = Type.GetType(outboxEvent.EventType);
        if (eventType == null)
        {
            throw new InvalidOperationException($"Unknown event type: {outboxEvent.EventType}");
        }

        var eventInstance = JsonSerializer.Deserialize(outboxEvent.EventData, eventType);
        if (eventInstance == null)
        {
            throw new InvalidOperationException($"Failed to deserialize event: {outboxEvent.EventType}");
        }

        await messageBus.PublishAsync(eventInstance, cancellationToken);
    }
}
```

## Transaction Isolation Levels

### Configure Isolation Levels

Set appropriate isolation levels for your operations:

```csharp
public static class CriticalReadHandler
{
    public static async Task<Result<CriticalData>> Handle(
        CriticalReadQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            // Perform critical read operations that require serializable isolation
            var data = await db.CriticalData
                .Where(cd => cd.TenantId == query.TenantId)
                .FirstOrDefaultAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            
            return data?.ToModel() ?? 
                   new List<ValidationFailure> { new("TenantId", "Critical data not found") };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public static class HighVolumeReadHandler
{
    public static async Task<Result<List<HighVolumeData>>> Handle(
        HighVolumeReadQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        using var transaction = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            // Use read committed for high-volume, less critical reads
            var data = await db.HighVolumeData
                .Where(hvd => hvd.TenantId == query.TenantId)
                .Skip(query.Skip)
                .Take(query.Take)
                .ToListAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            
            return data.Select(d => d.ToModel()).ToList();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

## Deadlock Detection and Handling

### Deadlock Retry Pattern

Handle deadlocks gracefully with retry logic:

```csharp
public static class DeadlockRetryHandler
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    public static async Task<TResult> ExecuteWithRetry<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        
        while (true)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (IsDeadlockException(ex) && attempt < MaxRetries)
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(RetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static bool IsDeadlockException(Exception ex)
    {
        // Check for PostgreSQL deadlock error codes
        return ex is Npgsql.PostgresException pgEx && 
               (pgEx.SqlState == "40001" || pgEx.SqlState == "40P01");
    }
}

// Usage
public static class UpdateWithRetryHandler
{
    public static async Task<(Result<Cashier>, CashierUpdated?)> Handle(
        UpdateCashierCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        return await DeadlockRetryHandler.ExecuteWithRetry(async ct =>
        {
            var dbCommand = CreateUpdateCommand(command);
            var updatedCashier = await messaging.InvokeCommandAsync(dbCommand, ct);

            var result = updatedCashier.ToModel();
            var updatedEvent = new CashierUpdated(result.TenantId, result);

            return (result, updatedEvent);
        }, cancellationToken);
    }
}
```

## Bulk Operations and Performance

### Bulk Insert with Transactions

Handle large data sets efficiently:

```csharp
public static class BulkCashierImportHandler
{
    public static async Task<Result<ImportResult>> Handle(
        BulkCashierImportCommand command,
        AppDomainDb db,
        ILogger<BulkCashierImportHandler> logger,
        CancellationToken cancellationToken)
    {
        using var transaction = await db.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var batchSize = 1000;
            var totalProcessed = 0;
            var errors = new List<string>();

            // Process in batches to avoid transaction timeout
            foreach (var batch in command.Cashiers.Chunk(batchSize))
            {
                var entities = batch.Select(c => new Data.Entities.Cashier
                {
                    TenantId = c.TenantId,
                    CashierId = Guid.CreateVersion7(),
                    Name = c.Name,
                    Email = c.Email,
                    CreatedDateUtc = DateTime.UtcNow,
                    UpdatedDateUtc = DateTime.UtcNow
                }).ToList();

                try
                {
                    // Use bulk copy for better performance
                    await db.BulkCopyAsync(entities, cancellationToken);
                    totalProcessed += entities.Count;
                    
                    logger.LogInformation("Processed batch of {BatchSize} cashiers, total: {Total}", 
                        entities.Count, totalProcessed);
                }
                catch (Exception ex)
                {
                    errors.Add($"Batch starting at record {totalProcessed}: {ex.Message}");
                    
                    // Decide whether to continue or fail
                    if (errors.Count > 10) // Too many errors
                    {
                        throw new InvalidOperationException($"Import failed with {errors.Count} batch errors");
                    }
                }
            }

            await transaction.CommitAsync(cancellationToken);

            return new ImportResult
            {
                TotalRecords = command.Cashiers.Count,
                ProcessedRecords = totalProcessed,
                ErrorCount = errors.Count,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Bulk import failed, transaction rolled back");
            
            return Result<ImportResult>.Failure($"Import failed: {ex.Message}");
        }
    }
}
```

## Testing with Transactions

### Transaction Testing Patterns

```csharp
[Test]
public async Task Handle_CreateCashier_CommitsTransactionOnSuccess()
{
    // Arrange
    using var connection = new NpgsqlConnection(TestConnectionString);
    await connection.OpenAsync();
    
    using var transaction = await connection.BeginTransactionAsync();
    var db = new AppDomainDb(connection);
    db.BeginTransaction(transaction);

    var command = new CreateCashierCommand(
        Guid.NewGuid(),
        "John Doe",
        "john@example.com");

    var mockMessaging = new Mock<IMessageBus>();
    mockMessaging.Setup(m => m.InvokeCommandAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((object cmd, CancellationToken ct) =>
             {
                 // Simulate database operation
                 if (cmd is CreateCashierCommandHandler.DbCommand dbCmd)
                 {
                     return dbCmd.Cashier;
                 }
                 throw new InvalidOperationException("Unexpected command type");
             });

    // Act
    var (result, integrationEvent) = await CreateCashierCommandHandler.Handle(
        command, mockMessaging.Object, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    integrationEvent.Should().NotBeNull();

    // Verify transaction can be committed
    await transaction.CommitAsync();
}

[Test]
public async Task Handle_CreateCashier_RollsBackTransactionOnError()
{
    // Arrange
    using var connection = new NpgsqlConnection(TestConnectionString);
    await connection.OpenAsync();
    
    using var transaction = await connection.BeginTransactionAsync();
    var db = new AppDomainDb(connection);
    db.BeginTransaction(transaction);

    var command = new CreateCashierCommand(
        Guid.NewGuid(),
        "John Doe",
        "john@example.com");

    var mockMessaging = new Mock<IMessageBus>();
    mockMessaging.Setup(m => m.InvokeCommandAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new InvalidOperationException("Database error"));

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
    {
        await CreateCashierCommandHandler.Handle(
            command, mockMessaging.Object, CancellationToken.None);
    });

    exception.Message.Should().Be("Database error");

    // Verify transaction was rolled back
    await transaction.RollbackAsync();
}
```

### Integration Tests with Transactions

```csharp
public class TransactionIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public TransactionIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CompleteWorkflow_Success_CommitsAllChanges()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var tenantId = Guid.NewGuid();

        // Act - Execute complete workflow
        var createCommand = new CreateCashierCommand(tenantId, "John Doe", "john@example.com");
        var (createResult, createEvent) = await messageBus.InvokeAsync(createCommand);

        var updateCommand = new UpdateCashierCommand(tenantId, createResult.Value.Id, "John Smith", "john.smith@example.com");
        var (updateResult, updateEvent) = await messageBus.InvokeAsync(updateCommand);

        // Assert - Verify all changes are persisted
        createResult.IsSuccess.Should().BeTrue();
        updateResult.IsSuccess.Should().BeTrue();

        var finalCashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.CashierId == createResult.Value.Id);

        finalCashier.Should().NotBeNull();
        finalCashier!.Name.Should().Be("John Smith");
        finalCashier.Email.Should().Be("john.smith@example.com");
    }

    [Fact]
    public async Task CompleteWorkflow_FailureInMiddle_RollsBackAllChanges()
    {
        // This test would verify that if any step in a multi-step operation fails,
        // the entire transaction is rolled back
    }
}
```

## Best Practices

### Transaction Management
1. **Keep transactions short**: Minimize transaction duration to reduce contention
2. **Use appropriate isolation levels**: Choose the right balance between consistency and performance
3. **Handle deadlocks gracefully**: Implement retry logic for deadlock scenarios
4. **Avoid nested transactions**: Use savepoints instead of nested transactions

### Event Publishing
1. **Use outbox pattern**: Ensure reliable event publishing with database transactions
2. **Handle publishing failures**: Implement retry logic for event publishing
3. **Monitor outbox events**: Set up alerts for unpublished events
4. **Clean up old events**: Regularly purge successfully published events

### Performance
1. **Use bulk operations**: For large data sets, use bulk inserts/updates
2. **Batch processing**: Process large operations in smaller batches
3. **Connection pooling**: Configure appropriate connection pool settings
4. **Monitor long-running transactions**: Alert on transactions that run too long

### Error Handling
1. **Always use try/catch**: Wrap transaction operations in proper error handling
2. **Log transaction errors**: Include sufficient context for debugging
3. **Provide meaningful errors**: Return helpful error messages to callers
4. **Implement circuit breakers**: Protect against cascading failures

## Next Steps

- Learn about [DbCommand Pattern](./dbcommand) for type-safe database operations
- Understand [Entity Mapping](./entity-mapping) for data transformation
- Explore [CQRS](../cqrs/) patterns for commands and queries
- See [Testing](../testing/) strategies for database operations