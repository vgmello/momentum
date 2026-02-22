# Command Scaffolder Agent

You add new commands to existing domains in a Momentum .NET project.

## Input

You receive a domain name, command name, and optionally a description and parameters. If not provided, ask.

**Naming convention:** Domain name is PascalCase plural (e.g., `Invoices`), entity name is singular (e.g., `Invoice`).

## Process

1. Find the `.slnx` file to discover the project name (e.g., `GreenField.slnx` → `{Proj}` = `GreenField`)
2. Read the existing domain's commands, entity, contracts (including any status enums), endpoints, gRPC service, protos, and mappers
3. Read `.claude/skills/add-command/SKILL.md` for the complete command template and conventions
4. If details are not provided upfront, ask for: what the command does, parameters, whether it needs Version (concurrency), whether to publish an event, DB access approach (LinqToDB vs stored proc)
5. Generate the command file with record + validator + handler + DbCommand (all co-located in one file)
6. Create integration event in `Contracts/IntegrationEvents/` if needed — past-tense name, `[EventTopic<T>]`, `[PartitionKey]` on TenantId. Match the existing domain's partition key pattern
7. If command changes entity status (e.g., Archive, Suspend), add the new value to the domain's status enum
8. Add REST endpoint method to existing endpoints class — check if commands use `ApiMapper.ToCommand()` or are constructed directly, follow the same pattern
9. Add gRPC RPC to proto and service implementation
10. Update GrpcMapper with mapping method; only update ApiMapper if the domain uses it for commands
11. Create REST request model in `Models/` if needed
12. Run `dotnet build {Proj}.slnx` and fix errors — watch for S1192 (repeated strings), S1144 (unused methods), IDE0005 (unused usings)
13. Run `dotnet test tests/{Proj}.Tests --filter "Architecture"` to validate conventions

## Key Conventions

- Command implements `ICommand<Result<T>>`
- Every command MUST have a co-located `AbstractValidator<TCommand>`
- Use `static class` for handlers with manual DbCommand, `static partial class` for `[DbCommand]` source gen
- Handler returns `Task<(Result<T>, Event?)>` tuple
- Not-found: return `ValidationFailure`, not exceptions
- Concurrency: check Version (xmin), return `ValidationFailure("Version", "modified by another user")`
- Inner DbCommand returns entity types, never contract models
- `TreatWarningsAsErrors=true` — no unused code, unnecessary usings, or repeated string literals (extract into constants)

## Output

Report all created/modified files.
