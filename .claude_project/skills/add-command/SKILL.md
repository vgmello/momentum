---
name: add-command
description: Add a new command to an existing domain with full stack wiring (validator, handler, REST endpoint, gRPC, mappers)
---

# Add Command

## Usage

`/add-command {Domain} {CommandName}`

Example: `/add-command Invoices ArchiveInvoice`

## Instructions

You are adding a new command to an existing domain in a Momentum .NET project.
Arguments: domain name (PascalCase, e.g., `Invoices`) and command name (PascalCase verb+noun, e.g., `ArchiveInvoice`).

**Naming convention:** The domain name is the plural (e.g., `Invoices`), the entity name is the singular (e.g., `Invoice`).

## Important: Build Rules

This project uses `TreatWarningsAsErrors=true` with SonarAnalyzer. Watch for:

- **S1192**: Repeated string literals — extract into constants if used 3+ times (common in GrpcMapper error messages)
- **S1144**: Unused private methods — only include helper methods that are actually called
- **IDE0005**: Unnecessary `using` — only include imports that are used

### Step 1: Discover the Project and Read the Existing Domain

Find the `.slnx` file to discover the project name (e.g., `GreenField.slnx` → `{Proj}` = `GreenField`).

Read the existing domain files to understand its current structure:

- All files in `src/{Proj}/{Domain}/Commands/` — existing command patterns
- `src/{Proj}/{Domain}/Data/Entities/` — the entity
- `src/{Proj}/{Domain}/Contracts/Models/` — the domain model (including any status enums)
- `src/{Proj}/{Domain}/Contracts/IntegrationEvents/` — existing event patterns (check if they use simple `[PartitionKey]` or compound `[PartitionKey(Order = N)]`)
- `src/{Proj}.Api/{Domain}/{Entity}Endpoints.cs` — existing REST routes
- `src/{Proj}.Api/{Domain}/{Entity}Service.cs` — existing gRPC methods
- `src/{Proj}.Api/{Domain}/Protos/` — existing proto definitions
- `src/{Proj}.Api/{Domain}/Mappers/` — existing mapper methods

### Step 2: Ask the User

If the user has already provided all details upfront, skip the questions and proceed. Otherwise, gather:

1. **What does this command do?** (brief description of the business operation)
2. **What parameters does it accept?** (beyond TenantId and EntityId which are always included)
3. **Does it need optimistic concurrency (Version field)?** Default: yes for mutations on existing entities
4. **Should it publish an integration event?** Default: yes — event name derived from command (e.g., `ArchiveInvoice` -> `InvoiceArchived`)
5. **Does it call a PostgreSQL function or use LinqToDB directly?** Default: LinqToDB for simple operations, `[DbCommand(fn: "$main.fn_name")]` for complex DB logic

### Step 3: Generate the Command File

Create `src/{Proj}/{Domain}/Commands/{CommandName}.cs` containing all three artifacts co-located:

```csharp
using {Proj}.{Domain}.Contracts.IntegrationEvents;
using {Proj}.{Domain}.Contracts.Models;
using {Proj}.{Domain}.Data;

namespace {Proj}.{Domain}.Commands;

// 1. Command record
public record {CommandName}Command(Guid TenantId, Guid {Entity}Id, /* params */) : ICommand<Result<{Entity}>>;

// 2. Validator — every command MUST have one
public class {CommandName}Validator : AbstractValidator<{CommandName}Command>
{
    public {CommandName}Validator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.{Entity}Id).NotEmpty();
        // Add rules for other parameters
    }
}

// 3. Handler — static class for LinqToDB direct, static partial class for [DbCommand] source gen
public static class {CommandName}CommandHandler
{
    // Nested DbCommand for persistence isolation
    public record DbCommand(/* params */) : ICommand<Data.Entities.{Entity}?>;

    // Business logic handler — returns tuple with optional event
    public static async Task<(Result<{Entity}>, {EventName}?)> Handle(
        {CommandName}Command command, IMessageBus messaging, CancellationToken cancellationToken)
    {
        var dbCommand = new DbCommand(/* map from command */);
        var result = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (result is null)
        {
            List<ValidationFailure> failures = [new("{Entity}Id", "{Entity} not found")];
            return (failures, null);
        }

        var model = result.ToModel();
        var evt = new {EventName}(model.TenantId, model);

        return (model, evt);
    }

    // DB handler — only for manual nested DbCommand
    public static async Task<Data.Entities.{Entity}?> Handle(
        DbCommand command, {Proj}Db db, CancellationToken cancellationToken)
    {
        // LinqToDB operation
        // For updates with concurrency: .Where(e => e.Version == command.Version)
    }
}
```

**For stored proc pattern** (use `static partial class` instead):

```csharp
public static partial class {CommandName}CommandHandler
{
    [DbCommand(fn: "$main.{snake_case_fn}")]
    public partial record DbCommand(/* params */) : ICommand<Data.Entities.{Entity}?>;

    public static async Task<(Result<{Entity}>, {EventName}?)> Handle(
        {CommandName}Command command, IMessageBus messaging, CancellationToken cancellationToken)
    {
        // Same pattern but no manual DB Handle method needed
    }
}
```

### Step 4: Create the Integration Event (if applicable)

Create `src/{Proj}/{Domain}/Contracts/IntegrationEvents/{EventName}.cs`:

```csharp
using {Proj}.{Domain}.Contracts.Models;

namespace {Proj}.{Domain}.Contracts.IntegrationEvents;

[EventTopic<{Entity}>]
public record {EventName}(
    [PartitionKey] Guid TenantId,
    {Entity} {Entity}
);
```

**Match the existing domain's partition key pattern.** If existing events use compound keys (`[PartitionKey(Order = 0)]` + `[PartitionKey(Order = 1)]`), follow that pattern.

Event name MUST be past tense (e.g., `InvoiceArchived`, `CashierDeactivated`).

### Step 5: Wire Up REST Endpoint

Add to `src/{Proj}.Api/{Domain}/{Entity}Endpoints.cs`:

- New route registration in `Map{Entity}Endpoints()` method
- New handler method following the existing pattern (extract TenantId from `context.User.GetTenantId()`, construct command, dispatch via `IMessageBus`, match result)
- Create a request model in `Models/{CommandName}Request.cs` if needed
- **REST route naming**: Use lowercase verb-based sub-routes (e.g., `PUT /{id}/archive` for ArchiveInvoice)

**Note**: Check the existing endpoints to see if commands are mapped via `ApiMapper.ToCommand()` or constructed directly in the handler. Follow the same pattern — do NOT add an ApiMapper method if the domain constructs commands directly.

### Step 6: Wire Up gRPC

- Add the RPC method to `src/{Proj}.Api/{Domain}/Protos/{domain_lower}.proto`
- Add request message with appropriate fields to the proto
- Add the override method to `src/{Proj}.Api/{Domain}/{Entity}Service.cs` following the existing pattern (extract TenantId from `context.GetTenantId()`, map via GrpcMapper, dispatch, match result)

### Step 7: Update Mappers

- Add mapping method to `GrpcMapper.cs`: `public static partial {CommandName}Command ToCommand(this {GrpcRequest} request, Guid tenantId);` or manual method if Guid parsing is needed
- Only add to `ApiMapper.cs` if the existing domain uses ApiMapper for command mapping (check existing patterns first)
- Watch for SonarQube S1192 when adding manual methods with repeated error strings — extract into constants

### Step 8: Verify

1. Run `dotnet build {Proj}.slnx` and fix any compilation errors or warnings.
2. Run `dotnet test tests/{Proj}.Tests --filter "Architecture"` to verify architecture rules pass.

### Additional Considerations

- **Status enum changes**: If your command transitions the entity to a new status (e.g., Archive, Suspend), add the new value to the domain's status enum (e.g., `InvoiceStatus.cs`).
- **Domain events**: For internal-only events (not published to Kafka), use `[EventTopic<T>(Internal = true)]` and place in `Contracts/DomainEvents/`.

### Conventions Checklist

- [ ] Command implements `ICommand<Result<T>>`
- [ ] Validator extends `AbstractValidator<TCommand>` with appropriate rules
- [ ] Handler is `static class` (LinqToDB direct) or `static partial class` (`[DbCommand]` source gen)
- [ ] Handler returns `Task<(Result<T>, Event?)>` tuple
- [ ] Event name is past tense (matches command verb)
- [ ] Event has `[EventTopic<T>]` and `[PartitionKey]` — match the existing domain's pattern
- [ ] Event lives in `Contracts/IntegrationEvents/` namespace
- [ ] Not-found returns `ValidationFailure` (not exceptions)
- [ ] Concurrency conflicts check Version via `xmin` and return `ValidationFailure("Version", "modified by another user")`
- [ ] Inner DbCommand returns entity type, not contract model
- [ ] REST endpoint extracts TenantId from `context.User.GetTenantId()`
- [ ] gRPC service extracts TenantId from `context.GetTenantId()`
- [ ] Proto uses `string` for GUIDs, `google.protobuf.Timestamp` for dates
- [ ] No unused `using` statements, private methods, or repeated string literals
