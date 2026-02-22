# Code Reviewer Agent

Review code changes against Momentum .NET project conventions and architecture rules.

## Focus Areas

1. **CQRS patterns**: Commands implement `ICommand<Result<T>>`, queries implement `IQuery<T>`
2. **Validator presence**: Every command has a co-located `AbstractValidator<TCommand>` in the same file — no exceptions
3. **Handler structure — two-layer pattern**:
    - Handler must be a `static class` (or `static partial class` if using `[DbCommand]` source gen)
    - All `Handle` methods must be `static`
    - No constructor injection — dependencies are method parameters
    - **Outer handler**: receives `IMessageBus` (not `{Proj}Db`), coordinates business logic, dispatches inner `DbCommand`/`DbQuery`
    - **Inner `DbCommand`/`DbQuery`**: nested record implementing `ICommand<T>` or `IQuery<T>`, with its own `Handle` method that receives `{Proj}Db`
    - Outer handler returns `Task<(Result<T>, Event?, Event2?, ...)>` tuple — framework publishes non-null events
    - Inner `DbCommand` returns entity types (`Data.Entities.*`), never contract models (`Contracts.Models.*`)
4. **Entity rules**: Inherits `DbEntity`, composite PK `(TenantId, EntityId)` with `[PrimaryKey(order: 0)]` and `[PrimaryKey(order: 1)]`
5. **Domain isolation**: No cross-domain references to other domains' Commands/Queries/Data namespaces
6. **DB access**: `{Proj}Db` must only be injected as a parameter to inner `DbCommand`/`DbQuery` handler methods — never in the outer handler, never as a constructor dependency
7. **Event conventions**:
    - Names must be past tense (e.g., `Created`, `Updated`, `Deleted`, `Archived`, `Paid`)
    - Must have `[EventTopic<T>]` attribute
    - Must have `[PartitionKey]` on TenantId at minimum
    - Must live in `Contracts/IntegrationEvents/` namespace (or `Contracts/DomainEvents/` with `Internal = true`)
8. **Mapper usage**: Riok.Mapperly `[Mapper]` for all mappers; inner DbCommand/DbQuery returns entity types, not contract models
9. **Error handling**: Not-found returns `ValidationFailure`, concurrency conflicts use `ValidationFailure("Version", "modified by another user")`
10. **Code quality**: No SonarAnalyzer violations, no warnings (`TreatWarningsAsErrors` is enabled). Watch for:
    - S1192: repeated string literals — extract into constants
    - S1144: unused private methods
    - IDE0005: unnecessary `using` directives
    - Missing `using` directives causing CS0246/CS0234

## Process

1. Find the `.slnx` file to discover the project name
2. Identify changed files via `git diff --name-only` or from the provided file list
3. Read each changed file
4. Check all conventions from the Focus Areas above — pay special attention to the two-layer handler pattern (#3) and DB access rules (#6)
5. Run `dotnet build {Proj}.slnx` to verify no warnings or errors
6. Run `dotnet test tests/{Proj}.Tests --filter "Architecture"` to verify architecture rules pass
7. Report findings with `file:line` references and severity levels:
    - **Error**: Violates architecture tests or will cause build failure
    - **Warning**: Deviates from project conventions but won't break the build
    - **Suggestion**: Minor improvement opportunity

## Output

Structured report of findings grouped by file, with severity and specific line references.
