# DbCommand Source Generator

## Generator Capabilities {#generator-capabilities}

This generator provides the following capabilities:

- **Parameter Mapping**: Generates `ToDbParams()` extension methods for parameter mapping
- **Command Handlers**: Creates Wolverine command handlers for database operations  
- **Multiple Command Types**: Supports stored procedures, SQL queries, and functions
- **Parameter Case Conversion**: Handles parameter case conversion (None, SnakeCase)
- **Custom Parameter Names**: Respects Column attributes for custom parameter names

The default parameter case can be configured via MSBuild property: `DbCommandDefaultParamCase`

## Generated Code Structure

### Parameter Extensions

For each type marked with `[DbCommand]`, the generator creates extension methods:

```csharp
public static class {TypeName}DbExtensions
{
    public static void ToDbParams(this {TypeName} source, DbParameterCollection parameters)
    {
        // Generated parameter mapping code
    }
}
```

### Command Handlers

When `Sp`, `Sql`, or `Fn` properties are specified, the generator creates Wolverine handlers:

```csharp
public class {TypeName}Handler
{
    public async Task<{ReturnType}> Handle({TypeName} command, [FromServices] IDbConnection connection)
    {
        // Generated database execution code
    }
}
```

## Configuration Options

### MSBuild Properties

Configure the generator behavior via MSBuild properties:

- **DbCommandDefaultParamCase**: Sets default parameter case conversion (None, SnakeCase)
- **DbCommandParamPrefix**: Adds a prefix to all generated parameter names

### Attribute Configuration

Control generation per type using the `DbCommandAttribute`:

```csharp
[DbCommand(
    Sp = "sp_create_user",           // Stored procedure name
    Sql = "INSERT INTO users...",    // Direct SQL query  
    Fn = "fn_calculate_total",       // Function name
    ParamCase = DbParamsCase.SnakeCase // Parameter case override
)]
public record CreateUserCommand(string Name, string Email);
```

## Code Generation Process

### 1. Type Discovery

The generator uses incremental source generation to discover types:

```csharp
var commandTypes = context.SyntaxProvider
    .ForAttributeWithMetadataName(
        fullyQualifiedMetadataName: "Momentum.Extensions.Abstractions.Dapper.DbCommandAttribute",
        predicate: static (_, _) => true,
        transform: static (ctx, _) => ctx.TargetSymbol as INamedTypeSymbol);
```

### 2. Settings Resolution

MSBuild properties are resolved and combined with attribute settings:

```csharp
var dbCommandSettings = GetDbCommandSettingsFromMsBuild(optionsProvider);
var typeInfo = new DbCommandTypeInfoSourceGen(namedTypeSymbol, dbCommandSettings);
```

### 3. Code Generation

Two separate generators run for each type:

- **DbExtensions**: Always generated for parameter mapping
- **Handler**: Only generated when database operation is specified

## Error Handling

The generator includes comprehensive error handling:

- **Diagnostic Reporting**: Reports compilation errors for invalid configurations
- **Early Exit**: Stops generation if errors are detected
- **Type Validation**: Ensures types are properly configured before generation

## Performance Considerations

- **Incremental Generation**: Only regenerates when source files change
- **Selective Generation**: Handlers only generated when needed
- **Compilation Optimization**: Uses efficient Roslyn APIs for type analysis