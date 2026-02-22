---
name: add-query
description: Add a new query to an existing domain with full stack wiring (handler, REST endpoint, gRPC, mappers)
---

# Add Query

## Usage

`/add-query {Domain} {QueryName}`

Example: `/add-query Invoices GetOverdueInvoices`

## Instructions

You are adding a new query to an existing domain in a Momentum .NET project.
Arguments: domain name (PascalCase) and query name (PascalCase Get+noun, e.g., `GetOverdueInvoices`).

**Naming convention:** The domain name is the plural (e.g., `Invoices`), the entity name is the singular (e.g., `Invoice`). The table name matches the plural (`Invoices`).

## Important: Build Rules

This project uses `TreatWarningsAsErrors=true` with SonarAnalyzer. Any unused code, unnecessary `using` statements, or unreachable code will cause build failures. Only include code that is actually used.

### Step 1: Discover the Project and Read the Existing Domain

Find the `.slnx` file to discover the project name (e.g., `GreenField.slnx` → `{Proj}` = `GreenField`).

Read:

- All files in `src/{Proj}/{Domain}/Queries/` — existing query patterns
- `src/{Proj}/{Domain}/Data/Entities/` — the entity
- `src/{Proj}/{Domain}/Contracts/Models/` — the domain model
- `src/{Proj}.Api/{Domain}/{Entity}Endpoints.cs` — existing REST routes
- `src/{Proj}.Api/{Domain}/{Entity}Service.cs` — existing gRPC methods
- `src/{Proj}.Api/{Domain}/Protos/` — proto definition
- `src/{Proj}.Api/{Domain}/Mappers/` — mappers

### Step 2: Ask the User

If the user has already provided all details upfront, skip the questions and proceed. Otherwise, gather:

1. **What does this query return?** Single entity or a list?
2. **What are the filter/input parameters?** (beyond TenantId which is always included)
3. **Does it need a PostgreSQL function or is LinqToDB LINQ sufficient?** Default: LinqToDB LINQ for simple filters, `[DbCommand(fn: "$main.fn")]` for complex aggregations or joins
4. **Does it need a custom result DTO?** Or return the standard domain model? (For custom DTOs, nest the result record inside the query record)

### Step 3: Generate the Query File

Create `src/{Proj}/{Domain}/Queries/{QueryName}.cs`:

**For a simple query (LinqToDB LINQ, single entity):**

```csharp
using LinqToDB.Async;
using {Proj}.{Domain}.Contracts.Models;
using {Proj}.{Domain}.Data;

namespace {Proj}.{Domain}.Queries;

public record {QueryName}Query(Guid TenantId, Guid {Entity}Id) : IQuery<Result<{Entity}>>;

public static class {QueryName}QueryHandler
{
    public static async Task<Result<{Entity}>> Handle(
        {QueryName}Query query, {Proj}Db db, CancellationToken cancellationToken)
    {
        var entity = await db.{Entities}
            .Where(e => e.TenantId == query.TenantId && e.{Entity}Id == query.{Entity}Id)
            .Take(1)
            .ToArrayAsync(cancellationToken);

        if (entity.Length > 0)
        {
            return entity[0].ToModel();
        }

        return new List<ValidationFailure> { new("{Entity}Id", "{Entity} not found") };
    }
}
```

**For a list query (LinqToDB LINQ):**

```csharp
using {Proj}.{Domain}.Contracts.Models;
using {Proj}.{Domain}.Data;

namespace {Proj}.{Domain}.Queries;

public record {QueryName}Query(Guid TenantId, /* filter params */, int Offset = 0, int Limit = 100)
    : IQuery<IEnumerable<{Entity}>>;

public static class {QueryName}QueryHandler
{
    public static async Task<IEnumerable<{Entity}>> Handle(
        {QueryName}Query query, {Proj}Db db, CancellationToken cancellationToken)
    {
        return await db.{Entities}
            .Where(e => e.TenantId == query.TenantId)
            // .Where(e => /* additional filters */)
            .OrderByDescending(e => e.CreatedDateUtc)
            .Skip(query.Offset)
            .Take(query.Limit)
            .Select(e => e.ToModel())
            .ToListAsync(cancellationToken);
    }
}
```

**For a stored proc query (use `static partial class`):**

```csharp
namespace {Proj}.{Domain}.Queries;

public record {QueryName}Query(Guid TenantId, int Offset = 0, int Limit = 100)
    : IQuery<IEnumerable<{QueryName}Query.Result>>
{
    // Nested result DTO if custom shape needed
    public record Result(Guid TenantId, Guid {Entity}Id, /* fields */);
}

public static partial class {QueryName}QueryHandler
{
    [DbCommand(fn: "$main.{snake_case_fn}")]
    public partial record DbQuery(Guid TenantId, int Limit, int Offset)
        : IQuery<IEnumerable<Data.Entities.{Entity}>>;

    public static async Task<IEnumerable<{QueryName}Query.Result>> Handle(
        {QueryName}Query query, IMessageBus messaging, CancellationToken cancellationToken)
    {
        var dbQuery = new DbQuery(query.TenantId, query.Limit, query.Offset);
        var entities = await messaging.InvokeQueryAsync(dbQuery, cancellationToken);

        return entities.Select(e => new {QueryName}Query.Result(/* map fields */));
    }
}
```

### Step 4: Wire Up REST Endpoint

Add to `src/{Proj}.Api/{Domain}/{Entity}Endpoints.cs`:

- New GET route in `Map{Entity}Endpoints()` method
- **REST route naming**: Use lowercase, descriptive sub-routes (e.g., `GET /invoices/overdue` for `GetOverdueInvoices`, `GET /invoices` for `GetInvoices`)
- Handler method: extract TenantId from `context.User.GetTenantId()`, map request to query, dispatch via `IMessageBus`, return result
- Use `[AsParameters]` attribute on the request model for query string binding in minimal API
- Create `Models/{QueryName}Request.cs` if the query has filter parameters beyond TenantId

### Step 5: Wire Up gRPC

- Add the RPC method to `src/{Proj}.Api/{Domain}/Protos/{domain_lower}.proto`
- Add request message with filter parameters
- For list queries, add a response wrapper: `message {QueryName}Response { repeated {Entity} {entities_lower} = 1; }`
- Add the override method to `src/{Proj}.Api/{Domain}/{Entity}Service.cs` following the existing pattern

### Step 6: Update Mappers

- Add mapping method to `ApiMapper.cs`: `public static partial {QueryName}Query ToQuery(this {Request} request, Guid tenantId);`
- Add mapping method to `GrpcMapper.cs`: `public static partial {QueryName}Query ToQuery(this {GrpcRequest} request, Guid tenantId);`
- Watch for SonarQube S1192 when adding manual methods with repeated error strings — extract into constants

### Step 7: Verify

1. Run `dotnet build {Proj}.slnx` and fix any compilation errors or warnings.
2. Run `dotnet test tests/{Proj}.Tests --filter "Architecture"` to verify architecture rules pass.

### Conventions Checklist

- [ ] Query implements `IQuery<T>` (either `IQuery<Result<T>>` for single entity or `IQuery<IEnumerable<T>>` for lists)
- [ ] Queries do NOT have validators (only commands do)
- [ ] Handler is `static class` (LinqToDB direct) or `static partial class` (`[DbCommand]` source gen)
- [ ] Simple queries inject `{Proj}Db` directly
- [ ] Complex queries use `[DbCommand]` with `IMessageBus`
- [ ] List queries support `Offset` and `Limit` parameters with sensible defaults
- [ ] Not-found returns `ValidationFailure`, not exceptions
- [ ] Custom result DTOs are nested inside the query record
- [ ] REST endpoint extracts TenantId from `context.User.GetTenantId()`
- [ ] gRPC service extracts TenantId from `context.GetTenantId()`
- [ ] gRPC list responses use a wrapper message with `repeated` field
- [ ] No unused `using` statements, private methods, or repeated string literals
