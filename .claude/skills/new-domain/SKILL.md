---
name: new-domain
description: Scaffold a new CQRS domain with Commands, Queries, Data, Contracts, and optional Orleans Actors
user-invocable: true
---

# Scaffold New CQRS Domain

Create a complete business domain following Momentum's CQRS patterns with Wolverine.

## Arguments

The user should provide:
- **name**: Domain name in PascalCase (e.g., `Products`, `Payments`, `Orders`)
- **entity**: Entity name in singular PascalCase (e.g., `Product`, `Payment`, `Order`). Defaults to singular of domain name.
- **properties**: Key entity properties (e.g., `Name:string, Amount:decimal, Status:string`)
- **orleans**: Whether to include Orleans actors (default: false)

## Domain Folder Structure

Create under `src/AppDomain/{DomainName}/`:

```
{DomainName}/
├── Commands/
│   └── Create{Entity}.cs           # Create command + validator + handler
├── Queries/
│   ├── Get{Entity}.cs              # Single entity query
│   └── Get{Entities}.cs            # List query with pagination
├── Data/
│   ├── Entities/
│   │   └── {Entity}.cs             # LinqToDB entity (inherits DbEntity)
│   └── DbMapper.cs                 # Mapperly mapper
├── Contracts/
│   ├── Models/
│   │   └── {Entity}.cs             # Domain model record
│   └── IntegrationEvents/
│       └── {Entity}Created.cs      # Integration event
└── Actors/ (optional, if orleans=true)
    ├── I{Entity}Actor.cs           # Grain interface
    └── {Entity}ActorState.cs       # Grain state
```

## File Templates

### Command: `Commands/Create{Entity}.cs`

```csharp
namespace AppDomain.{DomainName}.Commands;

/// <summary>Command to create a new {entity} in the system.</summary>
public record Create{Entity}Command(
    Guid TenantId,
    {properties}
) : ICommand<Result<{Entity}>>;

/// <summary>Validator for the Create{Entity}Command.</summary>
public class Create{Entity}Validator : AbstractValidator<Create{Entity}Command>
{
    public Create{Entity}Validator()
    {
        RuleFor(c => c.TenantId).NotEmpty().WithMessage("Tenant ID is required");
        // Add property validations
    }
}

/// <summary>Handler for the Create{Entity}Command.</summary>
public static class Create{Entity}CommandHandler
{
    /// <summary>Storage / persistence request</summary>
    public record DbCommand(Data.Entities.{Entity} {Entity}) : ICommand<Data.Entities.{Entity}>;

    public static async Task<(Result<{Entity}>, {Entity}Created?)> Handle(
        Create{Entity}Command command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = CreateInsertCommand(command);
        var inserted = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);
        var result = inserted.ToModel();
        var createdEvent = new {Entity}Created(command.TenantId, result);
        return (result, createdEvent);
    }

    public static async Task<Data.Entities.{Entity}> Handle(
        DbCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var inserted = await db.{Entities}.InsertWithOutputAsync(command.{Entity}, token: cancellationToken);
        return inserted;
    }

    private static DbCommand CreateInsertCommand(Create{Entity}Command command) =>
        new(new Data.Entities.{Entity}
        {
            TenantId = command.TenantId,
            {Entity}Id = Guid.CreateVersion7(),
            // Map properties
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        });
}
```

### Query: `Queries/Get{Entities}.cs`

```csharp
namespace AppDomain.{DomainName}.Queries;

/// <summary>Query to retrieve a paginated list of {entities}.</summary>
public record Get{Entities}Query(
    Guid TenantId,
    int Offset = 0,
    int Limit = 100
) : IQuery<IEnumerable<{Entity}>>;

public static class Get{Entities}QueryHandler
{
    public static async Task<IEnumerable<{Entity}>> Handle(
        Get{Entities}Query query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var results = await db.{Entities}
            .Where(e => e.TenantId == query.TenantId)
            .OrderByDescending(e => e.CreatedDateUtc)
            .Skip(query.Offset)
            .Take(query.Limit)
            .Select(e => e.ToModel())
            .ToListAsync(cancellationToken);
        return results;
    }
}
```

### Data Entity: `Data/Entities/{Entity}.cs`

```csharp
using LinqToDB.Mapping;

namespace AppDomain.{DomainName}.Data.Entities;

public record {Entity} : DbEntity
{
    [PrimaryKey(order: 0)]
    public Guid TenantId { get; set; }

    [PrimaryKey(order: 1)]
    public Guid {Entity}Id { get; set; }

    // Properties mapped from entity definition
}
```

If orleans=true, wrap with `[GenerateSerializer]`, `[Alias]`, and add `[Id(n)]` attributes.

### Contract Model: `Contracts/Models/{Entity}.cs`

```csharp
namespace AppDomain.{DomainName}.Contracts.Models;

/// <summary>Domain model representing a {entity}.</summary>
public record {Entity}(
    Guid TenantId,
    Guid {Entity}Id,
    {properties},
    DateTime CreatedDateUtc,
    DateTime UpdatedDateUtc,
    int Version
);
```

### Integration Event: `Contracts/IntegrationEvents/{Entity}Created.cs`

```csharp
namespace AppDomain.{DomainName}.Contracts.IntegrationEvents;

/// <summary>Published when a new {entity} is successfully created.</summary>
[EventTopic<{Entity}>]
public record {Entity}Created(
    [PartitionKey] Guid TenantId,
    {Entity} {Entity}
);
```

### Mapper: `Data/DbMapper.cs`

```csharp
using Riok.Mapperly.Abstractions;

namespace AppDomain.{DomainName}.Data;

[Mapper]
public static partial class DbMapper
{
    [MapperIgnoreSource(nameof(Entities.{Entity}.CreatedDateUtc))]
    [MapperIgnoreSource(nameof(Entities.{Entity}.UpdatedDateUtc))]
    public static partial {Entity} ToModel(this Entities.{Entity} entity);
}
```

## Post-Scaffold Steps

After creating the domain files:

1. **Register the table** in `AppDomainDb.cs`:
   ```csharp
   public ITable<{Entity}> {Entities} => this.GetTable<{Entity}>();
   ```

2. **Create the database migration** using `/create-migration` skill

3. **Add API endpoints** if the API project exists (in `src/AppDomain.Api/`)

4. **Run architecture tests** to verify compliance:
   ```bash
   dotnet test --filter "CqrsPatternRulesTests"
   dotnet test --filter "DomainIsolationRulesTests"
   ```

## Key Conventions

- **Namespace**: `AppDomain.{DomainName}.{SubFolder}`
- **Handlers**: Static classes with static `Handle` methods (Wolverine convention)
- **Validators**: FluentValidation `AbstractValidator<T>`
- **Return types**: `Result<T>` for commands, tuples with events for side effects
- **Events**: Use `[EventTopic<T>]` for integration events, `[EventTopic<T>(Internal = true)]` for domain events
- **Partition keys**: `[PartitionKey]` on the TenantId parameter
- **Mapping**: Mapperly `[Mapper]` for compile-time entity-to-model mapping
- **DB entity base**: Inherit from `DbEntity` (provides `CreatedDateUtc`, `UpdatedDateUtc`, `Version`)
