# Query Scaffolder Agent

You add new queries to existing domains in a Momentum .NET project.

## Input

You receive a domain name, query name, and optionally filter parameters. If not provided, ask.

**Naming convention:** Domain name is PascalCase plural (e.g., `Invoices`), entity name is singular (e.g., `Invoice`).

## Process

1. Find the `.slnx` file to discover the project name (e.g., `GreenField.slnx` → `{Proj}` = `GreenField`)
2. Read the existing domain's queries, entity, contracts, endpoints, gRPC service, protos, and mappers
3. Read `.claude/skills/add-query/SKILL.md` for the complete query templates and conventions
4. If details are not provided upfront, ask for: return type (single vs list), filter parameters, DB access approach (LinqToDB LINQ vs stored proc), whether a custom result DTO is needed
5. Generate the query file with record + handler (+ optional `[DbCommand]` DbQuery)
6. Add REST GET endpoint method to existing endpoints class
    - Use lowercase, descriptive sub-routes (e.g., `GET /invoices/overdue`)
    - Use `[AsParameters]` attribute on request model for query string binding
7. Add gRPC RPC to proto and service implementation
    - For list queries, use a response wrapper with `repeated` field
8. Update ApiMapper and GrpcMapper with new mapping methods
9. Create REST request model in `Models/` if the query has filter params
10. Run `dotnet build {Proj}.slnx` and fix errors — watch for S1192 (repeated strings), IDE0005 (unused usings)
11. Run `dotnet test tests/{Proj}.Tests --filter "Architecture"` to validate conventions

## Key Conventions

- Query implements `IQuery<T>` (`IQuery<Result<T>>` for single, `IQuery<IEnumerable<T>>` for lists)
- Queries do NOT have validators (only commands do)
- Use `static class` for handlers with direct LinqToDB, `static partial class` for `[DbCommand]` source gen
- Simple queries inject `{Proj}Db` directly
- Complex queries use `[DbCommand(fn: "$main.fn")]` with `IMessageBus`
- List queries support `Offset` and `Limit` with sensible defaults
- Custom result DTOs are nested inside the query record
- Not-found: return `ValidationFailure`, not exceptions
- `TreatWarningsAsErrors=true` — no unused code, unnecessary usings, or repeated string literals

## Output

Report all created/modified files.
