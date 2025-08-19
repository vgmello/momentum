# DbCommandAttribute Detailed Guide

## Generated Code Behavior

This attribute triggers the source generator to create:

1. **ToDbParams() Extension Method:** Converts class properties to Dapper-compatible parameter objects
2. **Command Handler Method:** Static async method that executes the database command (when sp/sql/fn provided)

## Command Handler Generation

Handlers are generated as static methods in a companion class (e.g., CreateUserCommandHandler.HandleAsync) that:

- Accept the command object, DbDataSource, and CancellationToken
- Open database connections automatically
- Map parameters using the generated ToDbParams() method
- Execute appropriate Dapper methods based on return type and nonQuery setting
- Handle connection disposal and async patterns correctly

## Parameter Mapping Rules

- Record properties and primary constructor parameters are automatically mapped
- Parameter names follow the paramsCase setting (None, SnakeCase, or global default)
- Use [Column("custom_name")] attribute to override specific parameter names
- MSBuild property DbCommandParamPrefix adds global prefixes to all parameters

## Return Type Handling

- `ICommand<int/long>`: Returns row count (ExecuteAsync) or scalar value (ExecuteScalarAsync)
- `ICommand<TResult>`: Returns single object (QueryFirstOrDefaultAsync<TResult>)
- `ICommand<IEnumerable<TResult>>`: Returns collection (QueryAsync<TResult>)
- `ICommand` (no return type): Executes command without returning data (ExecuteAsync)

## MSBuild Integration

Global configuration through MSBuild properties:

- `DbCommandDefaultParamCase`: Sets default parameter case conversion (None, SnakeCase)
- `DbCommandParamPrefix`: Adds prefix to all generated parameter names

## Requirements

- Target class must implement ICommand&lt;TResult&gt; or IQuery&lt;TResult&gt; (or parameterless versions)
- Class must be partial if nested within another type
- Only one of sp, sql, or fn can be specified per command
- Assembly must reference Momentum.Extensions.SourceGenerators
