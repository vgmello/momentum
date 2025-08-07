# Commands in Momentum

Commands represent write operations in your application that change state. In Momentum, commands follow a specific pattern that ensures consistency, validation, and event publishing.

## Command Definition

Commands are immutable records that implement `ICommand<TResult>`:

```csharp
public record CreateCashierCommand(Guid TenantId, string Name, string Email) : ICommand<Result<Cashier>>;
```

### Command Characteristics

- **Immutable**: Commands are records that cannot be modified after creation
- **Typed**: Commands specify their return type through `ICommand<TResult>`
- **Descriptive**: Command names should clearly indicate the action (CreateX, UpdateX, DeleteX)
- **Focused**: Each command should have a single responsibility

## Basic Command Example

Here's a complete example from the AppDomain reference implementation:

```csharp
// Commands/CreateCashier.cs
using AppDomain.Cashiers.Contracts.IntegrationEvents;
using AppDomain.Cashiers.Contracts.Models;
using FluentValidation;

namespace AppDomain.Cashiers.Commands;

public record CreateCashierCommand(Guid TenantId, string Name, string Email) : ICommand<Result<Cashier>>;

public class CreateCashierValidator : AbstractValidator<CreateCashierCommand>
{
    public CreateCashierValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}

public static class CreateCashierCommandHandler
{
    public record DbCommand(Data.Entities.Cashier Cashier) : ICommand<Data.Entities.Cashier>;

    public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
        CreateCashierCommand command, 
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = CreateInsertCommand(command);
        var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedCashier.ToModel();
        var createdEvent = new CashierCreated(result.TenantId, PartitionKeyTest: 0, result);

        return (result, createdEvent);
    }

    public static async Task<Data.Entities.Cashier> Handle(
        DbCommand command, 
        AppDomainDb db, 
        CancellationToken cancellationToken)
    {
        return await db.Cashiers.InsertWithOutputAsync(command.Cashier, token: cancellationToken);
    }

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

## Command Handler Pattern

### Two-Tier Handler Structure

Momentum uses a two-tier handler structure:

1. **Main Handler** - Business logic and orchestration
2. **Database Handler** - Pure database operations

This separation provides several benefits:
- **Testability**: You can unit test business logic separately from database operations
- **Reusability**: Database commands can be reused across different handlers
- **Clarity**: Clear separation of concerns

### Main Handler Signature

The main handler always returns a tuple with the result and optional integration event:

```csharp
public static async Task<(Result<T>, IntegrationEvent?)> Handle(
    TCommand command, 
    IMessageBus messaging,
    CancellationToken cancellationToken)
```

### Database Handler Signature

Database handlers perform the actual data operations:

```csharp
public static async Task<TEntity> Handle(
    DbCommand command, 
    TDbContext db, 
    CancellationToken cancellationToken)
```

## Command Validation

Commands are automatically validated using FluentValidation before the handler executes:

```csharp
public class UpdateCashierValidator : AbstractValidator<UpdateCashierCommand>
{
    public UpdateCashierValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MinimumLength(2)
            .WithMessage("Name must be at least 2 characters")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters");
    }
}
```

### Validation Features

- **Automatic execution**: Validation runs before the handler
- **Early return**: Invalid commands return validation errors immediately
- **Custom messages**: Provide user-friendly error messages
- **Complex rules**: Support for conditional validation and cross-field validation

## Advanced Command Patterns

### Update Commands

Update commands typically need to fetch existing data first:

```csharp
public record UpdateCashierCommand(Guid TenantId, Guid Id, string Name, string Email) : ICommand<Result<Cashier>>;

public static class UpdateCashierCommandHandler
{
    public record DbCommand(Data.Entities.Cashier Cashier) : ICommand<Data.Entities.Cashier>;

    public static async Task<(Result<Cashier>, CashierUpdated?)> Handle(
        UpdateCashierCommand command, 
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // First, get the existing cashier
        var getQuery = new GetCashierQuery(command.TenantId, command.Id);
        var existingResult = await messaging.InvokeAsync(getQuery, cancellationToken);

        if (!existingResult.IsSuccess)
        {
            return (existingResult, null);
        }

        var existing = existingResult.Value;
        
        // Create update command
        var dbCommand = CreateUpdateCommand(command, existing);
        var updatedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = updatedCashier.ToModel();
        var updatedEvent = new CashierUpdated(result.TenantId, result);

        return (result, updatedEvent);
    }

    public static async Task<Data.Entities.Cashier> Handle(
        DbCommand command, 
        AppDomainDb db, 
        CancellationToken cancellationToken)
    {
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

    private static DbCommand CreateUpdateCommand(UpdateCashierCommand command, Cashier existing) =>
        new(new Data.Entities.Cashier
        {
            TenantId = existing.TenantId,
            CashierId = existing.Id,
            Name = command.Name,
            Email = command.Email,
            CreatedDateUtc = existing.CreatedDate,
            UpdatedDateUtc = DateTime.UtcNow
        });
}
```

### Delete Commands

Delete commands should verify the entity exists before deletion:

```csharp
public record DeleteCashierCommand(Guid TenantId, Guid Id) : ICommand<Result<bool>>;

public static class DeleteCashierCommandHandler
{
    public record DbCommand(Guid TenantId, Guid CashierId) : ICommand<int>;

    public static async Task<(Result<bool>, CashierDeleted?)> Handle(
        DeleteCashierCommand command, 
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // Verify the cashier exists
        var getQuery = new GetCashierQuery(command.TenantId, command.Id);
        var existingResult = await messaging.InvokeAsync(getQuery, cancellationToken);

        if (!existingResult.IsSuccess)
        {
            return (existingResult.Errors, null);
        }

        var dbCommand = new DbCommand(command.TenantId, command.Id);
        var deletedCount = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (deletedCount > 0)
        {
            var deletedEvent = new CashierDeleted(command.TenantId, command.Id);
            return (true, deletedEvent);
        }

        return (new List<ValidationFailure> { new("Id", "Cashier could not be deleted") }, null);
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

## Error Handling

Commands use the `Result<T>` pattern for consistent error handling:

### Success Results

```csharp
// Implicit conversion from value to Result<T>
return cashier; // Automatically becomes Result<Cashier>.Success(cashier)
```

### Error Results

```csharp
// From validation failures
return new List<ValidationFailure> 
{ 
    new("Property", "Error message") 
};

// From custom errors
return Result<Cashier>.Failure("Custom error message");
```

## Integration Events

Commands can publish integration events to notify other services:

```csharp
public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
    CreateCashierCommand command, 
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // ... command logic ...

    var result = insertedCashier.ToModel();
    
    // Create integration event
    var createdEvent = new CashierCreated(
        TenantId: result.TenantId,
        PartitionKeyTest: 0,
        Cashier: result
    );

    return (result, createdEvent);
}
```

The integration event will be automatically published by the framework if the command succeeds.

## Best Practices

### Command Design

1. **Use descriptive names**: `CreateCashierCommand`, not `CashierCommand`
2. **Keep commands focused**: One command should do one thing
3. **Make commands immutable**: Always use records
4. **Include tenant context**: Multi-tenant applications should include `TenantId`

### Handler Design

1. **Separate concerns**: Keep business logic in the main handler, database operations in DbCommand handlers
2. **Use transactions**: Database operations should be atomic
3. **Validate early**: Use FluentValidation for input validation
4. **Handle errors gracefully**: Return meaningful error messages

### Validation Rules

1. **Validate all inputs**: Every command should have a validator
2. **Provide clear messages**: Users should understand validation errors
3. **Use async rules sparingly**: Prefer fast, synchronous validation
4. **Group related rules**: Use `RuleSet` for different validation scenarios

### Event Publishing

1. **Only publish on success**: Integration events should only be published when commands succeed
2. **Include necessary data**: Events should contain all data needed by consumers
3. **Use partition keys**: Ensure event ordering with appropriate partition keys
4. **Document events**: Use XML documentation for integration events

## Testing Commands

See our [Testing Guide](../testing/) for comprehensive examples of testing commands, including:

- Unit testing command handlers
- Testing validation rules
- Integration testing with databases
- Mocking dependencies

## Next Steps

- Learn about [Queries](./queries) for read operations
- Understand [Handlers](./handlers) in more detail
- Explore [Validation](./validation) patterns
- See [Integration Events](../messaging/integration-events) for cross-service communication