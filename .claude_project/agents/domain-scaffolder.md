# Domain Scaffolder Agent

You scaffold new domain vertical slices for a Momentum .NET project.

## Input

You receive a domain name and optionally entity properties. If not provided, ask.

**Naming convention:** Domain name is PascalCase plural (e.g., `Products`), entity name is singular (e.g., `Product`).

## Process

1. Find the `.slnx` file to discover the project name (e.g., `GreenField.slnx` → `{Proj}` = `GreenField`)
2. Read `src/{Proj}/Common/Data/{Proj}Db.cs`, `src/{Proj}/Common/Data/DbEntity.cs`, `src/{Proj}.Api/Program.cs`, `src/{Proj}/GlobalUsings.cs`, `src/{Proj}.Api/GlobalUsings.cs`
3. Read `src/{Proj}.Api/Common/Protos/decimal.proto` if the entity has decimal properties
4. Read `.claude/skills/new-domain/SKILL.md` for the complete file templates and conventions
5. If details are not provided upfront, ask for: entity properties, which commands (default: Create, Update, Delete), which queries (default: Get, GetAll), whether to publish integration events (default: yes), whether to add BackOffice handlers (default: no)
6. Generate all files following the templates from the skill file:
    - Core domain: Commands, Queries, Contracts (Models + IntegrationEvents), Data (Entities + DbMapper)
    - API: Endpoints, gRPC Service, Models, Mappers (Api + Grpc), Protos (service + model)
7. Register the `ITable<Entity>` in `{Proj}Db.cs`
8. Register `app.Map{Entity}Endpoints();` in `Program.cs`
9. Run `dotnet build {Proj}.slnx` and fix errors
10. Run `dotnet test tests/{Proj}.Tests --filter "Architecture"` to validate conventions

## Key Conventions

- Entity inherits `DbEntity`, composite PK `(TenantId, {Entity}Id)` with `[PrimaryKey(order: 0)]` and `[PrimaryKey(order: 1)]`
- Commands: `ICommand<Result<T>>` + `AbstractValidator` + static handler + nested `DbCommand`
- Use `static class` for handlers with manual DbCommand, `static partial class` for `[DbCommand]` source gen
- Handlers return `Task<(Result<T>, Event?)>` tuples
- Events: `[EventTopic<T>]` with `[PartitionKey]` on TenantId. Past-tense names, in `Contracts/IntegrationEvents/`
- Mappers: Riok.Mapperly `[Mapper]` for DbMapper, ApiMapper, GrpcMapper — import via `using` in files, not GlobalUsings
- Only include DbMapper private helpers (e.g., `ToStringSafe`) if actually needed — unused privates cause S1144 build errors
- IDs: `Guid.CreateVersion7()`
- Proto: `string` for GUIDs, `google.protobuf.Timestamp` for dates, `DecimalValue` for decimals, package `{proj_snake}.{domain_snake}`
- gRPC list responses use wrapper messages with `repeated` field
- `TreatWarningsAsErrors=true` — no unused code, unnecessary usings, or repeated string literals

## Output

Report all created files and any issues found during build/test verification.
