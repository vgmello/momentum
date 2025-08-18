---
title: Running Database Queries and Commands
---

# DbCommand Usage Guide - Running Database Queries and Commands

This guide provides comprehensive coverage of using the `DbCommand` attribute to run queries and commands on the database.

## Overview

The `DbCommand` attribute works with source generators to automatically create database handlers, parameter mapping, and integrate seamlessly with the Wolverine messaging framework.

Key features:

-   Automatically generates database parameter mapping via `ToDbParams()` methods
-   Source generates command handlers for database operations using Dapper
-   Integrates with the Wolverine messaging framework
-   Supports stored procedures, raw SQL, and database functions
-   Provides flexible parameter naming conventions

## Basic Concepts

### DbCommand Attribute Structure

```csharp
[DbCommand(
    sp: "stored_procedure_name",        // Stored procedure (mutually exclusive)
    sql: "SELECT * FROM table",         // Raw SQL query (mutually exclusive)
    fn: "SELECT * FROM function",       // Function call (mutually exclusive)
    paramsCase: DbParamsCase.SnakeCase, // Parameter naming convention
    nonQuery: true,                     // Whether it's a non-query operation
    dataSource: "connectionKey"         // Data source key for multi-DB scenarios
)]
```

### Parameter Case Conversion

The `DbParamsCase` enum controls how C# property names are converted to database parameters:

-   **`None`**: Uses property names as-is (default)
-   **`SnakeCase`**: Converts `PropertyName` to `property_name`
-   **`Unset`**: Uses global MSBuild property `DbCommandDefaultParamCase`

### Custom Parameter Names

Override individual parameter names using the `[Column]` attribute:

```csharp
[DbCommand] // [!code highlight]
public partial record MyCommand(
    [Column("custom_id")] Guid Id, // [!code highlight]
    string Name
);
```

## Command Patterns

Commands represent write operations (INSERT, UPDATE, DELETE) and typically return the number of affected rows or a scalar value.

### 1. Stored Procedure Commands

Execute stored procedures with automatic parameter mapping:

<<< @/../src/AppDomain/Invoices/Commands/CreateInvoice.cs{31-32 cs slice:30-45}

**Source Generated Handler:**

```csharp
public static class InsertInvoiceCommandHandler
{
    public static async Task<int> HandleAsync(
        InsertInvoiceCommand command,
        DbDataSource datasource,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
        var dbParams = command.ToDbParams(); // [!code highlight]
        return await SqlMapper.ExecuteAsync(connection,
            new CommandDefinition("AppDomain.invoices_create", dbParams,
                commandType: CommandType.StoredProcedure, // [!code highlight]
                cancellationToken: cancellationToken));
    }
}
```

### 2. Raw SQL Commands

Execute custom SQL with parameter binding:

```csharp
[DbCommand(sql: "UPDATE invoices SET status = @Status WHERE id = @Id", nonQuery: true)]
public partial record UpdateInvoiceStatusCommand(
    Guid Id,
    string Status
) : ICommand<int>;
```

### 3. Function Commands

Call database functions with automatic parameter injection:

```csharp
// Function call with parameters automatically appended
[DbCommand(fn: "SELECT AppDomain.calculate_tax", nonQuery: false)]
public partial record CalculateTaxCommand(
    decimal Amount,
    string Region
) : ICommand<decimal>;

// Generated SQL: SELECT AppDomain.calculate_tax(@Amount, @Region)
```

### 4. Command Handler Integration

Commands integrate with the Wolverine messaging framework:

<<< @/../src/AppDomain/Invoices/Commands/CreateInvoice.cs{43-78}

## Query Patterns

Queries represent read operations (SELECT) and return data objects or collections.

### 1. Single Record Queries

Retrieve a single record with automatic mapping:

<<< @/../src/AppDomain/Invoices/Queries/GetInvoice.cs{14-15}

**Source Generated Handler:**

```csharp
public static class GetInvoiceDbQueryHandler
{
    public static async Task<InvoiceModel?> HandleAsync(
        GetInvoiceDbQuery command,
        DbDataSource datasource,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
        var dbParams = command.ToDbParams();
        return await SqlMapper.QueryFirstOrDefaultAsync<InvoiceModel>(connection,
            new CommandDefinition("select * from AppDomain.invoices_get_single(@InvoiceId)",
                dbParams, commandType: CommandType.Text, cancellationToken: cancellationToken));
    }
}
```

### 2. Multiple Record Queries

Retrieve collections with pagination support:

<<< @/../src/AppDomain/Invoices/Queries/GetInvoices.cs{13-14}

**Source Generated Handler:**

```csharp
public static class GetInvoicesDbQueryHandler
{
    public static async Task<IEnumerable<InvoiceModel>> HandleAsync(
        GetInvoicesDbQuery command,
        DbDataSource datasource,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
        var dbParams = command.ToDbParams();
        return await SqlMapper.QueryAsync<InvoiceModel>(connection,
            new CommandDefinition("select * from AppDomain.invoices_get(@Limit, @Offset, @Status)",
                dbParams, commandType: CommandType.Text, cancellationToken: cancellationToken));
    }
}
```

### 3. Custom Parameter Mapping

Handle custom parameter names and case conversion:

<<< @/../src/AppDomain/Cashiers/Queries/GetCashier.cs{17-18}

**Generated `ToDbParams()` method:**

```csharp
public object ToDbParams()
{
    var p = new
    {
        id = this.CashierId  // Property mapped to custom parameter name
    };
    return p;
}
```

### 4. Snake Case Parameter Conversion

Automatically convert C# property names to database naming conventions:

<<< @/../src/AppDomain/Cashiers/Queries/GetCashiers.cs{25-26}

With `DbParamsCase.SnakeCase`, this generates:

```csharp
public object ToDbParams()
{
    var p = new
    {
        limit = this.Limit,    // Converted to snake_case
        offset = this.Offset   // Converted to snake_case
    };
    return p;
}
```

## Advanced Usage Patterns

### 1. Result Type Handling

The source generator creates different Dapper method calls based on result types:

```csharp
// For ICommand<int> with nonQuery: true
return await SqlMapper.ExecuteAsync(connection, commandDefinition);

// For ICommand<int> with nonQuery: false
return await SqlMapper.ExecuteScalarAsync<int>(connection, commandDefinition);

// For IQuery<MyType>
return await SqlMapper.QueryFirstOrDefaultAsync<MyType>(connection, commandDefinition);

// For IQuery<IEnumerable<MyType>>
return await SqlMapper.QueryAsync<MyType>(connection, commandDefinition);
```

### 2. Multi-Database Support

Specify different data sources for different commands:

```csharp
[DbCommand(sp: "AppDomain.get_invoice", dataSource: "AppDomainDb")]
public partial record GetInvoiceFromAppDomainDb(Guid Id) : IQuery<Invoice>;

[DbCommand(sp: "analytics.get_metrics", dataSource: "AnalyticsDb")]
public partial record GetMetricsFromAnalyticsDb(DateTime From, DateTime To) : IQuery<Metrics>;
```

### 3. Error Handling in Handlers

Implement proper error handling and validation:

<<< @/../src/AppDomain/Invoices/Commands/CancelInvoice.cs{26-54}

### 4. Transaction Management

Use Wolverine's messaging framework for transactional operations:

```csharp
public static async Task<Result<ComplexOperation>> Handle(
    ComplexOperationCommand command,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // All database operations within the same handler
    // are automatically wrapped in a transaction
    await messaging.InvokeCommandAsync(new CreateInvoiceCommand(...), cancellationToken);
    await messaging.InvokeCommandAsync(new UpdateCustomerCommand(...), cancellationToken);
    await messaging.InvokeCommandAsync(new LogActivityCommand(...), cancellationToken);

    // If any operation fails, the entire transaction is rolled back
    return Result.Success();
}
```

## Complete Implementation Examples

### Example 1: Invoice Creation with Full Lifecycle

```csharp
// Domain Command
public record CreateInvoiceCommand(
    string Name,
    decimal Amount,
    string? Currency = "",
    DateTime? DueDate = null,
    Guid? CashierId = null
) : ICommand<Result<InvoiceModel>>;

// Database Command
[DbCommand(sp: "AppDomain.invoices_create", nonQuery: true)]
public partial record InsertInvoiceCommand(
    Guid InvoiceId,
    string Name,
    string Status,
    decimal Amount,
    string? Currency,
    DateTime? DueDate,
    Guid? CashierId
) : ICommand<int>;

// Handler Implementation
public static async Task<(Result<InvoiceModel>, InvoiceCreated)> Handle(
    CreateInvoiceCommand command,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    var invoiceId = Guid.CreateVersion7();

    // Execute database operation
    var insertCommand = new InsertInvoiceCommand(
        invoiceId, command.Name, "Draft", command.Amount,
        command.Currency, command.DueDate, command.CashierId);

    await messaging.InvokeCommandAsync(insertCommand, cancellationToken);

    // Build response
    var result = new InvoiceModel
    {
        InvoiceId = invoiceId,
        Name = command.Name,
        Status = "Draft",
        Amount = command.Amount,
        Currency = command.Currency,
        DueDate = command.DueDate,
        CashierId = command.CashierId,
        CreatedDateUtc = DateTime.UtcNow,
        UpdatedDateUtc = DateTime.UtcNow,
        Version = 1
    };

    var createdEvent = new InvoiceCreated(result);
    return (result, createdEvent);
}
```

### Example 2: Paginated Query with Filtering

```csharp
// Domain Query
public record GetInvoicesQuery(
    int Limit = 50,
    int Offset = 0,
    string? Status = null
) : IQuery<IEnumerable<InvoiceModel>>;

// Database Query
[DbCommand(fn: "select * from AppDomain.invoices_get")]
public partial record GetInvoicesDbQuery(
    int Limit,
    int Offset,
    string? Status
) : IQuery<IEnumerable<InvoiceModel>>;

// Handler Implementation
public static async Task<IEnumerable<InvoiceModel>> Handle(
    GetInvoicesQuery query,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    var dbQuery = new GetInvoicesDbQuery(query.Limit, query.Offset, query.Status);
    var invoices = await messaging.InvokeQueryAsync(dbQuery, cancellationToken);
    return invoices;
}
```

### Example 3: Direct Database Access Pattern

```csharp
// For cases where you need more control over the database operation
[DbCommand]
private sealed partial record DbCommand([Column("id")] Guid CashierId);

public static async Task<Cashier> Handle(
    GetCashierQuery query,
    NpgsqlDataSource dataSource,
    CancellationToken cancellationToken)
{
    await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

    const string sql = "SELECT cashier_id, name, email FROM AppDomain.cashiers WHERE cashier_id = @id";

    var cashier = await connection.QuerySingleOrDefaultAsync<Data.Entities.Cashier>(
        sql,
        new DbCommand(query.Id).ToDbParams());

    if (cashier is null)
    {
        throw new InvalidOperationException($"Cashier with ID {query.Id} not found");
    }

    return new Cashier
    {
        CashierId = cashier.CashierId,
        Name = cashier.Name,
        Email = cashier.Email ?? string.Empty
    };
}
```

## Best Practices

### 1. Handler Organization

-   **Separate Domain and Database Commands**: Keep business logic separate from database operations
-   **Use Partial Classes**: Leverage `partial` keyword for generated handlers
-   **Nest Database Commands**: Place database command records inside handler classes

### 2. Parameter Naming

-   **Use Consistent Conventions**: Choose between `None` and `SnakeCase` consistently
-   **Override When Needed**: Use `[Column]` attribute for legacy database compatibility
-   **Global Configuration**: Set `DbCommandDefaultParamCase` MSBuild property

### 3. Error Handling

-   **Validate Input**: Use FluentValidation for command validation
-   **Handle Not Found**: Check for null results in queries
-   **Return Meaningful Errors**: Use Result pattern for error propagation

### 4. Performance Considerations

-   **Use Appropriate Methods**: Choose between `QueryFirstOrDefault` and `QuerySingle`
-   **Limit Query Results**: Always implement pagination for list queries
-   **Connection Management**: Let the framework handle connection lifecycle

### 5. Testing Strategies

-   **Unit Test Handlers**: Test business logic separately from database operations
-   **Integration Test Commands**: Test complete database operations
-   **Mock Database**: Use test doubles for external dependencies

## Common Patterns and Anti-Patterns

### ✅ Good Patterns

### ❌ Anti-Patterns

## Integration with Wolverine Framework

The DbCommand attribute integrates seamlessly with Wolverine's messaging infrastructure:

### Message Bus Integration

```csharp
// Commands are automatically registered with Wolverine
await messaging.InvokeCommandAsync(new CreateInvoiceCommand(...), cancellationToken);
await messaging.InvokeQueryAsync(new GetInvoiceQuery(...), cancellationToken);
```

### Automatic Handler Discovery

The source generator creates handlers that are automatically discovered by Wolverine.

### Transaction Management

Wolverine automatically manages transactions for database operations:

```csharp
// All operations within a handler are transactional
public static async Task<Result> Handle(ComplexCommand command, IMessageBus messaging, CancellationToken cancellationToken)
{
    await messaging.InvokeCommandAsync(new DbCommand1(...), cancellationToken);
    await messaging.InvokeCommandAsync(new DbCommand2(...), cancellationToken);
    // If any operation fails, all are rolled back
}
```

## Configuration and Setup

### MSBuild Configuration

Set global parameter case convention, if needed (usually for Postgres users):

```xml
<PropertyGroup>
  <DbCommandDefaultParamCase>SnakeCase</DbCommandDefaultParamCase>
</PropertyGroup>
```

### Data Source Registration

Register multiple data sources:

```csharp
services.AddKeyedSingleton<DbDataSource>("AppDomainDb", sp =>
    NpgsqlDataSource.Create(connectionString));

services.AddKeyedSingleton<DbDataSource>("AnalyticsDb", sp =>
    NpgsqlDataSource.Create(analyticsConnectionString));
```

## Troubleshooting

### Common Issues

1. **Handler Not Found**: Ensure the class is `partial` and has the correct namespace
2. **Parameter Mismatch**: Check property names match database parameter expectations
3. **Connection Issues**: Verify data source registration and connection strings
4. **Type Mapping**: Ensure result types match database column types

### Debugging Generated Code

View generated code in Visual Studio:

-   Go to **Project Properties** → **Build** → **Advanced** → **Debugging Information** → **Full**
-   Generated files appear in **Dependencies** → **Analyzers** → **Momentum.Extensions.SourceGenerators**

## Summary

The DbCommand attribute provides a powerful, type-safe way to interact with databases while maintaining clean separation of concerns. By leveraging source generation, it eliminates boilerplate code while providing full control over database operations. The integration with Wolverine's messaging framework ensures transactional consistency and enables event-driven architecture patterns.

Key benefits:

-   **Reduced Boilerplate**: Automatic parameter mapping and handler generation
-   **Type Safety**: Compile-time validation of database operations
-   **Flexible Configuration**: Support for multiple databases and naming conventions
-   **Framework Integration**: Seamless integration with Wolverine messaging
-   **Performance**: Built on Dapper for optimal database performance

Use this guide as a reference for implementing database operations in your applications, and refer to the AppDomain domain examples for real-world usage patterns.
