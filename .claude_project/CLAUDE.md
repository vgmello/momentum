# AppDomain

## Build & Test Commands

- Build: `dotnet build AppDomain.slnx`
- Test all: `dotnet test AppDomain.slnx`
- Test unit only: `dotnet test tests/AppDomain.Tests`
- Test E2E: `dotnet test tests/AppDomain.Tests.E2E`
- Format: `dotnet format AppDomain.slnx`
- Docker (API): `docker compose --profile api up -d`
- Docker (BackOffice): `docker compose --profile backoffice up -d`
- Docker (DB only): `docker compose --profile db up -d`

## Architecture

- .NET 10, C# with vertical slice architecture
- CQRS via Momentum/Wolverine framework with convention-based handler discovery
- Dual API: REST (Minimal API) + gRPC
- PostgreSQL (LinqToDB + Dapper), Kafka messaging, Microsoft Orleans
- Multi-tenant: composite PK `(TenantId, EntityId)` on all tables

## Project Structure

- `src/AppDomain/` — Core domain (commands, queries, entities, contracts)
- `src/AppDomain.Api/` — REST endpoints + gRPC services
- `src/AppDomain.BackOffice/` — Background jobs + Kafka consumers
- `src/AppDomain.Contracts/` — Publishable NuGet (links source from core + protos)
- `src/AppDomain.AppHost/` — Aspire orchestration
- `tests/AppDomain.Tests/` — Unit + integration + architecture tests
- `infra/AppDomain.Database/` — Liquibase migrations

## Key Patterns

- **Commands**: `record XxxCommand : ICommand<Result<T>>` + `AbstractValidator` + static handler class with nested `DbCommand`
- **Queries**: `record XxxQuery : IQuery<Result<T>>` + static handler; use `[DbCommand(fn: "$schema.fn_name")]` for stored procs
- **Entities**: inherit `DbEntity` (schema=main, xmin-based optimistic concurrency)
- **Mappers**: Riok.Mapperly `[Mapper]` for compile-time mapping (DbMapper, ApiMapper, GrpcMapper)
- **Events**: `[EventTopic<T>]` + `[PartitionKey]` records in `Contracts/IntegrationEvents/`
- **Handler return**: `Task<(Result<T>, Event1?, Event2?)>` tuple — framework publishes non-null events
- **DB params**: `[DbCommand]` uses `SnakeCase` + `p_` prefix automatically

## Code Quality

- `TreatWarningsAsErrors=true`, SonarAnalyzer, EditorConfig enforced
- Architecture tests (NetArchTest): domain isolation, CQRS patterns, entity inheritance, DB access rules
- Every command must have a validator (enforced by test)

## Conventions

- Use `Guid.CreateVersion7()` for new IDs
- Static handler classes (no interface needed — convention-based discovery)
- Cashiers domain is the canonical reference for patterns
- REST endpoints: static class with `Map{Domain}Endpoints()` extension method
- gRPC services: class inheriting generated `{Service}ServiceBase`
