---
name: new-domain
description: Scaffold a complete vertical slice domain (REST + gRPC + commands + queries + entities + events + tests)
---

# New Domain Scaffolder

## Usage

`/new-domain {DomainName}`

Example: `/new-domain Products`

## Instructions

You are scaffolding a new domain vertical slice for a Momentum .NET project.
The argument is the domain name in PascalCase plural (e.g., `Products`, `Payments`, `Customers`).

**Naming convention:** The domain name is the plural (e.g., `Products`), the entity name is the singular (e.g., `Product`). The table name matches the plural (`Products`).

## Important: Build Rules

This project uses `TreatWarningsAsErrors=true` with SonarAnalyzer. Any unused code, unnecessary `using` statements, or unreachable code will cause build failures. Only include code that is actually used.

Watch for:

- **S1192**: Repeated string literals — extract into constants if used 3+ times
- **S1144**: Unused private methods — only include helper methods that are actually called
- **IDE0005**: Unnecessary `using` — only include imports that are used

## Proto Type Reference

| C# Type    | Proto Type                            | Notes                                 |
| ---------- | ------------------------------------- | ------------------------------------- |
| `Guid`     | `string`                              | Parse with `Guid.Parse()`             |
| `DateTime` | `google.protobuf.Timestamp`           | Use `ToTimestamp()` / `ToDateTime()`  |
| `decimal`  | `{project_snake}.common.DecimalValue` | Import `Common/Protos/decimal.proto`  |
| `int`      | `int32`                               |                                       |
| `string`   | `string`                              | Proto strings are inherently nullable |
| `bool`     | `bool`                                |                                       |

### Step 1: Discover the Project

Find the `.slnx` file in the project root to discover the project name:

```
{ProjectRoot}/*.slnx → e.g., "GreenField.slnx" → ProjectName = "GreenField"
```

Then read these files:

- `src/{Proj}/Common/Data/{Proj}Db.cs` — where to register the new table
- `src/{Proj}/Common/Data/DbEntity.cs` — base entity class
- `src/{Proj}/GlobalUsings.cs` — what's globally imported in core project
- `src/{Proj}.Api/GlobalUsings.cs` — what's globally imported in API project
- `src/{Proj}.Api/Program.cs` — where to register endpoints
- `src/{Proj}.Api/Common/Protos/decimal.proto` — only if entity has decimal properties

Derive these values from the project name:

- `{Proj}` = project name (e.g., `GreenField`)
- `{proj_snake}` = snake_case (e.g., `green_field`)
- `{Proj}Db` = DB class name (e.g., `GreenFieldDb`)
- `I{Proj}Assembly` = assembly marker (e.g., `IGreenFieldAssembly`)

### Step 2: Ask the User for Domain Details

If the user has already provided all details upfront, skip the questions and proceed to Step 3. Otherwise, gather:

1. **What entity properties does this domain have?** (name, type, nullable?)
2. **Which commands are needed?** Default: Create, Update, Delete — confirm or customize
3. **Which queries are needed?** Default: Get{Entity}, Get{Entities} — confirm or customize
4. **Should it publish integration events?** Default: yes (Created, Updated, Deleted)
5. **Does it need BackOffice event handlers?** Default: no

### Step 3: Generate All Files

Create all files following the templates below. Replace `{Proj}`, `{Domain}`, `{Entity}`, `{Entities}`, `{proj_snake}`, `{domain_snake}`, `{entity_snake}` with actual values.

---

#### 3.1 Entity — `src/{Proj}/{Domain}/Data/Entities/{Entity}.cs`

```csharp
using LinqToDB.Mapping;

namespace {Proj}.{Domain}.Data.Entities;

public record {Entity} : DbEntity
{
    [PrimaryKey(order: 0)]
    public Guid TenantId { get; set; }

    [PrimaryKey(order: 1)]
    public Guid {Entity}Id { get; set; }

    // Add entity-specific properties here
    // Use string? for nullable, string for required (with = string.Empty default)
}
```

#### 3.2 Contract Model — `src/{Proj}/{Domain}/Contracts/Models/{Entity}.cs`

```csharp
namespace {Proj}.{Domain}.Contracts.Models;

public record {Entity}
{
    public Guid TenantId { get; init; }
    public Guid {Entity}Id { get; init; }
    // Mirror entity properties as init-only
    public DateTime CreatedDateUtc { get; init; }
    public DateTime UpdatedDateUtc { get; init; }
    public int Version { get; init; }
}
```

#### 3.3 DbMapper — `src/{Proj}/{Domain}/Data/DbMapper.cs`

```csharp
using {Proj}.{Domain}.Contracts.Models;
using Riok.Mapperly.Abstractions;

namespace {Proj}.{Domain}.Data;

[Mapper]
public static partial class DbMapper
{
    public static partial {Entity} ToModel(this Entities.{Entity} entity);

    // ONLY include ToStringSafe if entity has nullable string mapped to required string:
    // private static string ToStringSafe(string? value) => value ?? string.Empty;
}
```

**IMPORTANT**: Only include the `ToStringSafe` helper if the entity actually has a `string?` property mapped to a `required string` in the model. Unused private methods cause S1144 build errors.

#### 3.4 Integration Events — `src/{Proj}/{Domain}/Contracts/IntegrationEvents/`

**{Entity}Created.cs:**

```csharp
using {Proj}.{Domain}.Contracts.Models;

namespace {Proj}.{Domain}.Contracts.IntegrationEvents;

[EventTopic<{Entity}>]
public record {Entity}Created(
    [PartitionKey] Guid TenantId,
    {Entity} {Entity}
);
```

**{Entity}Updated.cs:**

```csharp
using {Proj}.{Domain}.Contracts.Models;

namespace {Proj}.{Domain}.Contracts.IntegrationEvents;

[EventTopic<{Entity}>]
public record {Entity}Updated(
    [PartitionKey] Guid TenantId,
    {Entity} {Entity}
);
```

**{Entity}Deleted.cs:**

```csharp
using {Proj}.{Domain}.Contracts.Models;

namespace {Proj}.{Domain}.Contracts.IntegrationEvents;

[EventTopic<{Entity}>]
public record {Entity}Deleted(
    [PartitionKey] Guid TenantId,
    Guid {Entity}Id,
    DateTime DeletedAt
);
```

#### 3.5 Create Command — `src/{Proj}/{Domain}/Commands/Create{Entity}.cs`

```csharp
using {Proj}.{Domain}.Contracts.IntegrationEvents;
using {Proj}.{Domain}.Contracts.Models;
using {Proj}.{Domain}.Data;

namespace {Proj}.{Domain}.Commands;

public record Create{Entity}Command(Guid TenantId, /* entity properties */) : ICommand<Result<{Entity}>>;

public class Create{Entity}Validator : AbstractValidator<Create{Entity}Command>
{
    public Create{Entity}Validator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        // Add rules for each property
    }
}

public static class Create{Entity}CommandHandler
{
    public record DbCommand(Data.Entities.{Entity} {Entity}) : ICommand<Data.Entities.{Entity}>;

    public static async Task<(Result<{Entity}>, {Entity}Created?)> Handle(
        Create{Entity}Command command, IMessageBus messaging, CancellationToken cancellationToken)
    {
        var dbCommand = CreateInsertCommand(command);
        var inserted = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = inserted.ToModel();
        var createdEvent = new {Entity}Created(result.TenantId, result);

        return (result, createdEvent);
    }

    public static async Task<Data.Entities.{Entity}> Handle(
        DbCommand command, {Proj}Db db, CancellationToken cancellationToken)
    {
        return await db.{Entities}.InsertWithOutputAsync(command.{Entity}, cancellationToken);
    }

    private static DbCommand CreateInsertCommand(Create{Entity}Command command) =>
        new(new Data.Entities.{Entity}
        {
            TenantId = command.TenantId,
            {Entity}Id = Guid.CreateVersion7(),
            // Map command properties to entity
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        });
}
```

#### 3.6 Update Command — `src/{Proj}/{Domain}/Commands/Update{Entity}.cs`

```csharp
using {Proj}.{Domain}.Contracts.IntegrationEvents;
using {Proj}.{Domain}.Contracts.Models;
using {Proj}.{Domain}.Data;

namespace {Proj}.{Domain}.Commands;

public record Update{Entity}Command(Guid TenantId, Guid {Entity}Id, /* properties */, int Version) : ICommand<Result<{Entity}>>;

public class Update{Entity}Validator : AbstractValidator<Update{Entity}Command>
{
    public Update{Entity}Validator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.{Entity}Id).NotEmpty();
        // Add rules for each property
    }
}

public static class Update{Entity}CommandHandler
{
    public record DbCommand(Guid TenantId, Guid {Entity}Id, /* properties */, int Version) : ICommand<Data.Entities.{Entity}?>;

    public static async Task<(Result<{Entity}>, {Entity}Updated?)> Handle(
        Update{Entity}Command command, IMessageBus messaging, CancellationToken cancellationToken)
    {
        var dbCommand = new DbCommand(command.TenantId, command.{Entity}Id, /* pass properties */, command.Version);
        var updated = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (updated is null)
        {
            List<ValidationFailure> failures = [new("{Entity}Id", "{Entity} not found")];
            return (failures, null);
        }

        var result = updated.ToModel();
        var updatedEvent = new {Entity}Updated(result.TenantId, result);

        return (result, updatedEvent);
    }

    public static async Task<Data.Entities.{Entity}?> Handle(
        DbCommand command, {Proj}Db db, CancellationToken cancellationToken)
    {
        var statement = db.{Entities}
            .Where(e => e.TenantId == command.TenantId && e.{Entity}Id == command.{Entity}Id)
            .Where(e => e.Version == command.Version)
            // .Set(e => e.PropertyName, command.PropertyName) — for each property
            .Set(e => e.UpdatedDateUtc, DateTime.UtcNow);

        // For nullable properties: if (!string.IsNullOrWhiteSpace(command.Prop)) statement = statement.Set(...)

        var updatedRecords = statement.UpdateWithOutputAsync((_, inserted) => inserted);
        return await updatedRecords.FirstOrDefaultAsync(cancellationToken);
    }
}
```

#### 3.7 Delete Command — `src/{Proj}/{Domain}/Commands/Delete{Entity}.cs`

```csharp
using {Proj}.{Domain}.Contracts.IntegrationEvents;

namespace {Proj}.{Domain}.Commands;

public record Delete{Entity}Command(Guid TenantId, Guid {Entity}Id) : ICommand<Result<bool>>;

public class Delete{Entity}Validator : AbstractValidator<Delete{Entity}Command>
{
    public Delete{Entity}Validator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.{Entity}Id).NotEmpty();
    }
}

public static class Delete{Entity}CommandHandler
{
    public record DbCommand(Guid TenantId, Guid {Entity}Id) : ICommand<bool>;

    public static async Task<(Result<bool>, {Entity}Deleted?)> Handle(
        Delete{Entity}Command command, IMessageBus messaging, CancellationToken cancellationToken)
    {
        var dbCommand = new DbCommand(command.TenantId, command.{Entity}Id);
        var deleted = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (deleted)
        {
            var deletedEvent = new {Entity}Deleted(command.TenantId, command.{Entity}Id, DateTime.UtcNow);
            return (true, deletedEvent);
        }

        List<ValidationFailure> failures = [new("{Entity}Id", "{Entity} not found")];
        return (failures, null);
    }

    public static async Task<bool> Handle(
        DbCommand command, {Proj}Db db, CancellationToken cancellationToken)
    {
        var deletedCount = await db.{Entities}
            .Where(e => e.TenantId == command.TenantId && e.{Entity}Id == command.{Entity}Id)
            .DeleteAsync(cancellationToken);

        return deletedCount > 0;
    }
}
```

#### 3.8 Get Query — `src/{Proj}/{Domain}/Queries/Get{Entity}.cs`

```csharp
using LinqToDB.Async;
using {Proj}.{Domain}.Contracts.Models;
using {Proj}.{Domain}.Data;

namespace {Proj}.{Domain}.Queries;

public record Get{Entity}Query(Guid TenantId, Guid {Entity}Id) : IQuery<Result<{Entity}>>;

public static class Get{Entity}QueryHandler
{
    public static async Task<Result<{Entity}>> Handle(
        Get{Entity}Query query, {Proj}Db db, CancellationToken cancellationToken)
    {
        var entity = await db.{Entities}
            .Where(e => e.TenantId == query.TenantId && e.{Entity}Id == query.{Entity}Id)
            .Take(1)
            .ToArrayAsync(cancellationToken);

        if (entity.Length > 0)
        {
            return entity[0].ToModel();
        }

        return new List<ValidationFailure> { new("Id", "{Entity} not found") };
    }
}
```

#### 3.9 GetAll Query — `src/{Proj}/{Domain}/Queries/Get{Entities}.cs`

```csharp
namespace {Proj}.{Domain}.Queries;

public record Get{Entities}Query(Guid TenantId, int Offset = 0, int Limit = 100)
    : IQuery<IEnumerable<Get{Entities}Query.Result>>
{
    public record Result(Guid TenantId, Guid {Entity}Id, /* properties */, DateTime CreatedDateUtc, DateTime UpdatedDateUtc, int Version);
}

public static partial class Get{Entities}QueryHandler
{
    [DbCommand(fn: "$main.{entities_snake}_get_all")]
    public partial record DbQuery(Guid TenantId, int Limit, int Offset)
        : IQuery<IEnumerable<Data.Entities.{Entity}>>;

    public static async Task<IEnumerable<Get{Entities}Query.Result>> Handle(
        Get{Entities}Query query, IMessageBus messaging, CancellationToken cancellationToken)
    {
        var dbQuery = new DbQuery(query.TenantId, query.Limit, query.Offset);
        var entities = await messaging.InvokeQueryAsync(dbQuery, cancellationToken);

        return entities.Select(e => new Get{Entities}Query.Result(
            e.TenantId, e.{Entity}Id, /* map properties */,
            e.CreatedDateUtc, e.UpdatedDateUtc, e.Version));
    }
}
```

**Note**: Use `static partial class` for the handler because it uses `[DbCommand]` source generation.

#### 3.10 REST Endpoints — `src/{Proj}.Api/{Domain}/{Entity}Endpoints.cs`

```csharp
using {Proj}.Api.Common.Extensions;
using {Proj}.Api.{Domain}.Mappers;
using {Proj}.Api.{Domain}.Models;
using {Proj}.{Domain}.Commands;
using {Proj}.{Domain}.Contracts.Models;
using {Proj}.{Domain}.Queries;

namespace {Proj}.Api.{Domain};

public static class {Entity}Endpoints
{
    public static RouteGroupBuilder Map{Entity}Endpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("{entities_lower}")
            .WithTags("{Domain}");

        group.MapGet("/{id:guid}", Get{Entity})
            .WithName("Get{Entity}")
            .Produces<{Entity}>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapGet("/", Get{Entities})
            .WithName("Get{Entities}")
            .Produces<IEnumerable<Get{Entities}Query.Result>>()
            .ProducesValidationProblem();

        group.MapPost("/", Create{Entity})
            .WithName("Create{Entity}")
            .Produces<{Entity}>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", Update{Entity})
            .WithName("Update{Entity}")
            .Produces<{Entity}>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapDelete("/{id:guid}", Delete{Entity})
            .WithName("Delete{Entity}")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<IResult> Get{Entity}(Guid id, IMessageBus bus, HttpContext context,
        CancellationToken cancellationToken)
    {
        var query = new Get{Entity}Query(context.User.GetTenantId(), id);
        var result = await bus.InvokeQueryAsync(query, cancellationToken);

        return result.Match<IResult>(
            TypedResults.Ok,
            errors => TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, detail: errors[0].ErrorMessage));
    }

    private static async Task<IResult> Get{Entities}([AsParameters] Get{Entities}Request request, IMessageBus bus,
        HttpContext context, CancellationToken cancellationToken)
    {
        var query = request.ToQuery(context.User.GetTenantId());
        var result = await bus.InvokeQueryAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> Create{Entity}(Create{Entity}Request request, IMessageBus bus,
        HttpContext context, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(context.User.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, cancellationToken);

        return result.Match<IResult>(
            entity => TypedResults.Created($"/{entities_lower}/{entity.{Entity}Id}", entity),
            errors => TypedResults.ValidationProblem(errors.ToValidationErrors()));
    }

    private static async Task<IResult> Update{Entity}(Guid id, Update{Entity}Request request, IMessageBus bus,
        HttpContext context, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(context.User.GetTenantId(), id);
        var result = await bus.InvokeCommandAsync(command, cancellationToken);

        return result.Match<IResult>(
            TypedResults.Ok,
            errors => errors.IsConcurrencyConflict()
                ? TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, detail: errors[0].ErrorMessage)
                : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, detail: errors[0].ErrorMessage));
    }

    private static async Task<IResult> Delete{Entity}(Guid id, IMessageBus bus, HttpContext context,
        CancellationToken cancellationToken)
    {
        var command = new Delete{Entity}Command(context.User.GetTenantId(), id);
        var result = await bus.InvokeCommandAsync(command, cancellationToken);

        return result.Match<IResult>(
            _ => TypedResults.NoContent(),
            errors => TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, detail: errors[0].ErrorMessage));
    }
}
```

#### 3.11 REST Request Models — `src/{Proj}.Api/{Domain}/Models/`

**Create{Entity}Request.cs:**

```csharp
using System.Text.Json.Serialization;

namespace {Proj}.Api.{Domain}.Models;

public record Create{Entity}Request
{
    // [JsonRequired] for required properties
    // public required string Name { get; init; }
}
```

**Update{Entity}Request.cs:**

```csharp
using System.Text.Json.Serialization;

namespace {Proj}.Api.{Domain}.Models;

public record Update{Entity}Request
{
    // Same properties as Create but include Version
    [JsonRequired]
    public required int Version { get; init; }
}
```

**Get{Entities}Request.cs:**

```csharp
using System.ComponentModel.DataAnnotations;

namespace {Proj}.Api.{Domain}.Models;

public record Get{Entities}Request
{
    [Range(1, 100)]
    public int Limit { get; init; } = 100;

    [Range(0, int.MaxValue)]
    public int Offset { get; init; } = 0;
}
```

#### 3.12 ApiMapper — `src/{Proj}.Api/{Domain}/Mappers/ApiMapper.cs`

```csharp
using {Proj}.Api.{Domain}.Models;
using {Proj}.{Domain}.Commands;
using {Proj}.{Domain}.Queries;
using Riok.Mapperly.Abstractions;

namespace {Proj}.Api.{Domain}.Mappers;

[Mapper]
public static partial class ApiMapper
{
    public static partial Create{Entity}Command ToCommand(this Create{Entity}Request request, Guid tenantId);
    public static partial Update{Entity}Command ToCommand(this Update{Entity}Request request, Guid tenantId, Guid {entity}Id);
    public static partial Get{Entities}Query ToQuery(this Get{Entities}Request request, Guid tenantId);
}
```

#### 3.13 GrpcMapper — `src/{Proj}.Api/{Domain}/Mappers/GrpcMapper.cs`

```csharp
using {Proj}.Api.Common.Extensions;
using {Proj}.{Domain}.Commands;
using {Proj}.{Domain}.Contracts.Models;
using {Proj}.{Domain}.Grpc;
using {Proj}.{Domain}.Queries;
using Google.Protobuf.WellKnownTypes;
using Riok.Mapperly.Abstractions;
using Grpc{Entity} = {Proj}.{Domain}.Grpc.Models.{Entity};

namespace {Proj}.Api.{Domain}.Mappers;

[Mapper]
public static partial class GrpcMapper
{
    public static partial Grpc{Entity} ToGrpc(this {Entity} source);
    public static partial Grpc{Entity} ToGrpc(this Get{Entities}Query.Result source);
    public static partial Create{Entity}Command ToCommand(this Create{Entity}Request request, Guid tenantId);

    [MapperIgnoreSource(nameof(Update{Entity}Request.{Entity}Id))]
    public static partial Update{Entity}Command ToCommand(this Update{Entity}Request request, Guid tenantId, Guid {entity}Id);

    public static Delete{Entity}Command ToCommand(this Delete{Entity}Request request, Guid tenantId)
        => new(tenantId, request.{Entity}Id.ToGuidSafe("Invalid {entity} ID format"));

    public static partial Get{Entities}Query ToQuery(this Get{Entities}Request request, Guid tenantId);

    public static Get{Entity}Query ToQuery(this Get{Entity}Request request, Guid tenantId)
        => new(tenantId, request.Id.ToGuidSafe("Invalid {entity} ID format"));

    private static string ToString(Guid guid) => guid.ToString();
    private static Timestamp ToTimestamp(DateTime dateTime) => dateTime.ToUniversalTime().ToTimestamp();
}
```

#### 3.14 gRPC Service — `src/{Proj}.Api/{Domain}/{Entity}Service.cs`

```csharp
using {Proj}.Api.Common.Extensions;
using {Proj}.Api.{Domain}.Mappers;
using {Proj}.{Domain}.Grpc;
using Google.Protobuf.WellKnownTypes;
using {Entity}Model = {Proj}.{Domain}.Grpc.Models.{Entity};

namespace {Proj}.Api.{Domain};

public class {Entity}Service(IMessageBus bus) : {Domain}Service.{Domain}ServiceBase
{
    public override async Task<{Entity}Model> Get{Entity}(Get{Entity}Request request, ServerCallContext context)
    {
        var query = request.ToQuery(context.GetTenantId());
        var result = await bus.InvokeQueryAsync(query, context.CancellationToken);

        return result.Match(
            entity => entity.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.NotFound, string.Join("; ", errors))));
    }

    public override async Task<Get{Entities}Response> Get{Entities}(Get{Entities}Request request, ServerCallContext context)
    {
        var query = request.ToQuery(context.GetTenantId());
        var entities = await bus.InvokeQueryAsync(query, context.CancellationToken);

        return new Get{Entities}Response
        {
            {Entities} = { entities.Select(e => e.ToGrpc()) }
        };
    }

    public override async Task<{Entity}Model> Create{Entity}(Create{Entity}Request request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            entity => entity.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors))));
    }

    public override async Task<{Entity}Model> Update{Entity}(Update{Entity}Request request, ServerCallContext context)
    {
        var {entity}Id = request.{Entity}Id.ToGuidSafe("Invalid {entity} ID format");
        var command = request.ToCommand(context.GetTenantId(), {entity}Id);
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            entity => entity.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors))));
    }

    public override async Task<Empty> Delete{Entity}(Delete{Entity}Request request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            _ => new Empty(),
            errors => throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors))));
    }
}
```

#### 3.15 Proto Service — `src/{Proj}.Api/{Domain}/Protos/{domain_lower}.proto`

```protobuf
syntax = "proto3";

import "{Domain}/Protos/Models/{entity_lower}.proto";
import "google/protobuf/empty.proto";

option csharp_namespace = "{Proj}.{Domain}.Grpc";

package {proj_snake}.{domain_snake};

service {Domain}Service {
  rpc Get{Entity} (Get{Entity}Request) returns ({proj_snake}.{domain_snake}.{Entity});
  rpc Get{Entities} (Get{Entities}Request) returns (Get{Entities}Response);
  rpc Create{Entity} (Create{Entity}Request) returns ({proj_snake}.{domain_snake}.{Entity});
  rpc Update{Entity} (Update{Entity}Request) returns ({proj_snake}.{domain_snake}.{Entity});
  rpc Delete{Entity} (Delete{Entity}Request) returns (google.protobuf.Empty);
}

message Get{Entity}Request {
  string id = 1;
}

message Get{Entities}Request {
  int32 limit = 1;
  int32 offset = 2;
}

message Get{Entities}Response {
  repeated {proj_snake}.{domain_snake}.{Entity} {entities_lower} = 1;
}

message Create{Entity}Request {
  // Add fields matching entity properties (NOT TenantId, NOT {Entity}Id)
}

message Update{Entity}Request {
  string {entity_snake}_id = 1;
  // Add fields matching entity properties
  int32 version = N;  // last field number
}

message Delete{Entity}Request {
  string {entity_snake}_id = 1;
}
```

#### 3.16 Proto Model — `src/{Proj}.Api/{Domain}/Protos/Models/{entity_lower}.proto`

```protobuf
syntax = "proto3";

import "google/protobuf/timestamp.proto";

option csharp_namespace = "{Proj}.{Domain}.Grpc.Models";

package {proj_snake}.{domain_snake};

message {Entity} {
  string tenant_id = 1;
  string {entity_snake}_id = 2;
  // Add fields matching entity properties
  google.protobuf.Timestamp created_date_utc = N;
  google.protobuf.Timestamp updated_date_utc = N;
  int32 version = N;
}
```

### Step 4: Register in Infrastructure

#### 4.1 Add table to `{Proj}Db`

Add to `src/{Proj}/Common/Data/{Proj}Db.cs`:

```csharp
using {Proj}.{Domain}.Data.Entities;
// ...
public ITable<{Entity}> {Entities} => this.GetTable<{Entity}>();
```

#### 4.2 Add GlobalUsings in Contracts project (if needed)

If the `src/{Proj}.Contracts/GlobalUsings.cs` does NOT already have `Momentum.Extensions.Abstractions.Messaging`, add it:

```csharp
global using Momentum.Extensions.Abstractions.Messaging;
```

This is needed because integration event files use `[EventTopic<T>]` and `[PartitionKey]` attributes which are globally imported in the core project but NOT in the Contracts project (which compiles the same source files via `<Compile Include>`).

#### 4.3 Register endpoints in `Program.cs`

Add to `src/{Proj}.Api/Program.cs`:

```csharp
using {Proj}.Api.{Domain};
// ...
app.Map{Entity}Endpoints();
```

Add this line before `app.MapDefaultHealthCheckEndpoints();`.

### Step 5: Verify

1. Run `dotnet build {Proj}.slnx` and fix any compilation errors or warnings.
2. Run `dotnet test tests/{Proj}.Tests --filter "Architecture"` to verify architecture rules pass.

### Conventions Checklist

- [ ] Entity inherits `DbEntity`, composite PK `(TenantId, {Entity}Id)` with `[PrimaryKey(order: 0)]` and `[PrimaryKey(order: 1)]`
- [ ] All commands implement `ICommand<Result<T>>`
- [ ] Every command has a co-located `AbstractValidator<TCommand>`
- [ ] Handlers are `static class` (or `static partial class` if using `[DbCommand]` source gen)
- [ ] Commands return `Task<(Result<T>, Event?)>` tuples
- [ ] Events use `[EventTopic<T>]` with `[PartitionKey]` on TenantId
- [ ] Event names are past tense (Created, Updated, Deleted)
- [ ] Events live in `Contracts/IntegrationEvents/` namespace
- [ ] Domain models live in `Contracts/Models/` namespace
- [ ] DbMapper uses Riok.Mapperly `[Mapper]` — no unused private helpers
- [ ] REST endpoints: static class, `Map{Entity}Endpoints()` extension
- [ ] gRPC service inherits generated `{Domain}Service.{Domain}ServiceBase`
- [ ] IDs generated with `Guid.CreateVersion7()`
- [ ] Proto uses `string` for GUIDs, `google.protobuf.Timestamp` for dates
- [ ] Proto package follows `{proj_snake}.{domain_snake}` convention
- [ ] Inner DbCommand returns entity types, not contract models
- [ ] Not-found errors return `ValidationFailure`, not exceptions
- [ ] No unused `using` statements or private methods (TreatWarningsAsErrors)
- [ ] Mapper `using` added to individual files, not GlobalUsings
