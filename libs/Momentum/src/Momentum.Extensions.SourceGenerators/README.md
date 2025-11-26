# Momentum.Extensions.SourceGenerators

**Database Command Source Generators** for the Momentum platform that automatically generate Dapper-based database access code, eliminating boilerplate and ensuring type-safe parameter mapping for your commands and queries.

## Overview

The `Momentum.Extensions.SourceGenerators` package provides a powerful **DbCommand source generator** that analyzes your command and query classes marked with `[DbCommand]` attributes and automatically generates:

-   **Parameter Providers**: Type-safe `ToDbParams()` methods for database parameter mapping
-   **Command Handlers**: Complete database execution handlers with proper async patterns
-   **Dapper Integration**: Seamless integration with Dapper for high-performance data access
-   **Multiple Database Patterns**: Support for stored procedures, SQL queries, and database functions

**Key Benefits:**

-   **Zero Runtime Overhead**: All code generation happens at compile-time
-   **Type Safety**: Strongly-typed parameter mapping with compile-time validation
-   **Reduced Boilerplate**: Eliminates repetitive database access code
-   **Consistent Patterns**: Enforces consistent data access patterns across your application
-   **IDE Support**: Generated code appears in IntelliSense and debugging

## Installation & Setup

### Package Installation

Add the package to your project:

```bash
dotnet add package Momentum.Extensions.SourceGenerators
```

### Required Dependencies

The source generator requires these companion packages:

```xml
<PackageReference Include="Momentum.Extensions.Abstractions" Version="1.0.0" />
<PackageReference Include="Dapper" Version="2.1.35" />
<PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
```

### Project Configuration

Add to your `.csproj` file:

```xml
<PropertyGroup>
  <!-- Configure default parameter case conversion -->
  <DbCommandDefaultParamCase>None</DbCommandDefaultParamCase>

  <!-- Optional: Enable generated file output for debugging -->
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>

  <!-- Optional: Enable verbose generator logging -->
  <MomentumGeneratorVerbose>false</MomentumGeneratorVerbose>
</PropertyGroup>
```

## DbCommand Generator - Complete Guide

### Basic Usage Pattern

The DbCommand generator follows this pattern:

1. **Define your command/query** as a `partial record` implementing `ICommand<T>` or `IQuery<T>`
2. **Decorate with `[DbCommand]`** specifying stored procedure, SQL, or function
3. **Generated code provides** parameter mapping and database execution logic

### 1. Stored Procedure Commands

**Basic Stored Procedure Example:**

```csharp
using Momentum.Extensions.Abstractions.Dapper;
using Momentum.Extensions.Abstractions.Messaging;

[DbCommand(sp: "create_user", nonQuery: true)]
public partial record CreateUserCommand(int UserId, string Name) : ICommand<int>;
```

**Generated Parameter Provider:**

```csharp
sealed public partial record CreateUserCommand : IDbParamsProvider
{
    public object ToDbParams()
    {
        return this; // Uses record properties directly
    }
}
```

**Generated Handler:**

```csharp
public static class CreateUserCommandHandler
{
    public static async Task<int> HandleAsync(
        CreateUserCommand command,
        DbDataSource datasource,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
        var dbParams = command.ToDbParams();
        return await SqlMapper.ExecuteAsync(connection,
            new CommandDefinition("create_user", dbParams,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
```

### 2. SQL Query Commands

**SQL Query with Object Result:**

```csharp
[DbCommand(sql: "SELECT * FROM users WHERE id = @UserId")]
public partial record GetUserByIdQuery(int UserId) : ICommand<User>;

public record User(int Id, string Name, string Email);
```

**Generated Handler for Single Object:**

```csharp
public static class GetUserByIdQueryHandler
{
    public static async Task<User> HandleAsync(
        GetUserByIdQuery command,
        DbDataSource datasource,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
        var dbParams = command.ToDbParams();
        return await SqlMapper.QueryFirstOrDefaultAsync<User>(connection,
            new CommandDefinition("SELECT * FROM users WHERE id = @UserId", dbParams,
                commandType: CommandType.Text,
                cancellationToken: cancellationToken));
    }
}
```

**SQL Query with Collection Result:**

```csharp
[DbCommand(sql: "SELECT * FROM users WHERE active = @Active")]
public partial record GetActiveUsersQuery(bool Active) : ICommand<IEnumerable<User>>;
```

**Generated Handler for Collections:**

```csharp
public static class GetActiveUsersQueryHandler
{
    public static async Task<IEnumerable<User>> HandleAsync(
        GetActiveUsersQuery command,
        DbDataSource datasource,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
        var dbParams = command.ToDbParams();
        return await SqlMapper.QueryAsync<User>(connection,
            new CommandDefinition("SELECT * FROM users WHERE active = @Active", dbParams,
                commandType: CommandType.Text,
                cancellationToken: cancellationToken));
    }
}
```

### 3. Database Function Commands

**Function with Auto-Generated Parameters:**

```csharp
[DbCommand(fn: "select * from app_domain.invoices_get")]
public partial record GetInvoicesQuery(int Limit, int Offset, string Status) : IQuery<IEnumerable<Invoice>>;

public record Invoice(int Id, string Status, decimal Amount);
```

**Generated SQL with Function Parameters:**

```csharp
public static class GetInvoicesQueryHandler
{
    public static async Task<IEnumerable<Invoice>> HandleAsync(
        GetInvoicesQuery command,
        DbDataSource datasource,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
        var dbParams = command.ToDbParams();
        // Note: Function parameters are automatically appended
        return await SqlMapper.QueryAsync<Invoice>(connection,
            new CommandDefinition("select * from app_domain.invoices_get(@Limit, @Offset, @Status)",
                dbParams, commandType: CommandType.Text,
                cancellationToken: cancellationToken));
    }
}
```

### 4. Parameter Mapping Patterns

**Default Parameter Mapping (Property Names As-Is):**

```csharp
[DbCommand(sp: "update_user")]
public partial record UpdateUserCommand(int UserId, string FirstName, string LastName) : ICommand<int>;

// Generated ToDbParams() returns: { UserId, FirstName, LastName }
```

**Snake Case Parameter Mapping:**

```csharp
[DbCommand(sp: "update_user", paramsCase: DbParamsCase.SnakeCase)]
public partial record UpdateUserCommand(int UserId, string FirstName, string LastName) : ICommand<int>;
```

**Generated with Snake Case:**

```csharp
public object ToDbParams()
{
    var p = new
    {
        user_id = this.UserId,
        first_name = this.FirstName,
        last_name = this.LastName
    };
    return p;
}
```

**Custom Parameter Names with Column Attribute:**

```csharp
using System.ComponentModel.DataAnnotations.Schema;

[DbCommand(sp: "update_user", paramsCase: DbParamsCase.SnakeCase)]
public partial record UpdateUserCommand(
    int UserId,
    [Column("custom_name")] string FirstName,
    string LastName,
    [Column("email_address")] string EmailAddr
) : ICommand<int>;
```

**Generated with Custom Names:**

```csharp
public object ToDbParams()
{
    var p = new
    {
        user_id = this.UserId,
        custom_name = this.FirstName,    // Uses Column attribute
        last_name = this.LastName,
        email_address = this.EmailAddr   // Uses Column attribute
    };
    return p;
}
```

## Advanced Usage Scenarios

### 1. Scalar Return Types

**Integer Results (Row Count or Scalar):**

```csharp
// Non-query: Returns rows affected
[DbCommand(sp: "delete_inactive_users", nonQuery: true)]
public partial record DeleteInactiveUsersCommand() : ICommand<int>;

// Scalar query: Returns actual count value
[DbCommand(sql: "SELECT COUNT(*) FROM users")]
public partial record GetUserCountQuery() : ICommand<int>;
```

**Long Scalar Results:**

```csharp
[DbCommand(sql: "SELECT @@IDENTITY")]
public partial record GetLastInsertIdQuery() : ICommand<long>;

// Generated handler uses ExecuteScalarAsync<long>
```

### 2. Multiple Data Sources

**Keyed Data Source Injection:**

```csharp
[DbCommand(sp: "get_report", dataSource: "ReportingDb")]
public partial record GetReportQuery(int ReportId) : ICommand<Report>;

public record Report(int Id, string Title, DateTime CreatedDate);
```

**Generated Handler with Keyed Service:**

```csharp
public static async Task<Report> HandleAsync(
    GetReportQuery command,
    [FromKeyedServices("ReportingDb")] DbDataSource datasource,
    CancellationToken cancellationToken = default)
{
    // Implementation uses the keyed data source
}
```

### 3. Complex Parameter Scenarios

**Commands with No Parameters:**

```csharp
[DbCommand(sp: "cleanup_temp_data")]
public partial record CleanupTempDataCommand() : ICommand<int>;

// Generated ToDbParams() returns 'this' (empty record)
```

**Commands with Optional/Nullable Parameters:**

```csharp
[DbCommand(sql: "SELECT * FROM users WHERE (@Name IS NULL OR name LIKE @Name) AND (@MinAge IS NULL OR age >= @MinAge)")]
public partial record SearchUsersQuery(string? Name, int? MinAge) : ICommand<IEnumerable<User>>;

// Nullable parameters are handled automatically by Dapper
```

### 4. Global Configuration

**MSBuild Configuration:**

```xml
<PropertyGroup>
  <!-- Set default parameter case for all DbCommand attributes -->
  <DbCommandDefaultParamCase>SnakeCase</DbCommandDefaultParamCase>

  <!-- Enable verbose logging for debugging -->
  <MomentumGeneratorVerbose>true</MomentumGeneratorVerbose>
</PropertyGroup>
```

**Per-Command Override:**

```csharp
// This command uses None case despite global SnakeCase setting
[DbCommand(sp: "legacy_proc", paramsCase: DbParamsCase.None)]
public partial record LegacyCommand(int UserId) : ICommand<int>;
```

## Integration Patterns

### 1. Dependency Injection Setup

**Service Registration:**

```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register DbDataSource
    services.AddNpgsqlDataSource(connectionString);

    // Register Wolverine for command handling
    services.AddWolverine(opts =>
    {
        opts.Discovery.DisableConventionalDiscovery();
        // Generated handlers are discovered automatically
    });
}
```

### 2. Usage in Application Services

**Sending Commands through Message Bus:**

```csharp
public class UserService
{
    private readonly IMessageBus _messageBus;

    public UserService(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public async Task<int> CreateUserAsync(string name)
    {
        var command = new CreateUserCommand(UserId: 0, Name: name);
        return await _messageBus.InvokeAsync(command);
    }

    public async Task<User> GetUserAsync(int userId)
    {
        var query = new GetUserByIdQuery(userId);
        return await _messageBus.InvokeAsync(query);
    }
}
```

**Direct Handler Usage:**

```csharp
public class UserRepository
{
    private readonly DbDataSource _dataSource;

    public UserRepository(DbDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<User> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var query = new GetUserByIdQuery(userId);
        return await GetUserByIdQueryHandler.HandleAsync(query, _dataSource, cancellationToken);
    }
}
```

### 3. Repository Pattern Integration

**Generated Commands as Repository Methods:**

```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(int userId);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<int> CreateAsync(string name);
    Task<int> UpdateAsync(int userId, string firstName, string lastName);
}

public class UserRepository : IUserRepository
{
    private readonly DbDataSource _dataSource;

    public UserRepository(DbDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<User> GetByIdAsync(int userId)
    {
        var query = new GetUserByIdQuery(userId);
        return await GetUserByIdQueryHandler.HandleAsync(query, _dataSource);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        var query = new GetActiveUsersQuery(true);
        return await GetActiveUsersQueryHandler.HandleAsync(query, _dataSource);
    }

    public async Task<int> CreateAsync(string name)
    {
        var command = new CreateUserCommand(0, name);
        return await CreateUserCommandHandler.HandleAsync(command, _dataSource);
    }

    public async Task<int> UpdateAsync(int userId, string firstName, string lastName)
    {
        var command = new UpdateUserCommand(userId, firstName, lastName);
        return await UpdateUserCommandHandler.HandleAsync(command, _dataSource);
    }
}
```

## Configuration & Debugging

### MSBuild Properties

```xml
<PropertyGroup>
  <!-- Global parameter case setting -->
  <DbCommandDefaultParamCase>None|SnakeCase</DbCommandDefaultParamCase>

  <!-- Enable generator debugging -->
  <MomentumGeneratorVerbose>true</MomentumGeneratorVerbose>

  <!-- Output generated files for inspection -->
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

### Viewing Generated Code

**In Visual Studio:**

1. **Solution Explorer** → **Dependencies** → **Analyzers** → **Momentum.Extensions.SourceGenerators**
2. Expand to see generated `.g.cs` files

**File System Location:**

```
obj/Debug/net10.0/generated/Momentum.Extensions.SourceGenerators/
├── CreateUserCommand.DbExt.g.cs     # Parameter provider
├── CreateUserCommandHandler.g.cs    # Command handler
├── GetUserByIdQuery.DbExt.g.cs      # Parameter provider
└── GetUserByIdQueryHandler.g.cs     # Query handler
```

**Enable File Output:**

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

### Debugging Generated Code

**Enable Verbose Logging:**

```xml
<PropertyGroup>
  <MomentumGeneratorVerbose>true</MomentumGeneratorVerbose>
</PropertyGroup>
```

**Build with Diagnostic Output:**

```bash
dotnet build -v diagnostic
```

**Generator Debugging (Advanced):**

```bash
# Enable generator debugging in environment
export DOTNET_EnableSourceGeneratorDebugging=1
dotnet build
```

## Troubleshooting Guide

### Common Issues & Solutions

**1. Generator Not Running**

```csharp
// ❌ Problem: Generator not creating code
[DbCommand(sp: "test_proc")]
public record TestCommand(int Id) : ICommand<int>; // Missing 'partial'

// ✅ Solution: Add 'partial' keyword
[DbCommand(sp: "test_proc")]
public partial record TestCommand(int Id) : ICommand<int>;
```

**2. Compilation Errors**

```bash
# Error: IDbParamsProvider not found
# Solution: Add required package reference
dotnet add package Momentum.Extensions.Abstractions
```

**3. Parameter Mapping Issues**

```csharp
// ❌ Problem: Parameter name mismatch
[DbCommand(sql: "SELECT * FROM users WHERE user_id = @UserId")]
public partial record GetUserQuery(int UserId) : ICommand<User>; // Expects @UserId, but DB uses user_id

// ✅ Solution: Use Column attribute or snake_case
[DbCommand(sql: "SELECT * FROM users WHERE user_id = @user_id", paramsCase: DbParamsCase.SnakeCase)]
public partial record GetUserQuery(int UserId) : ICommand<User>;

// OR use Column attribute
[DbCommand(sql: "SELECT * FROM users WHERE user_id = @user_id")]
public partial record GetUserQuery([Column("user_id")] int UserId) : ICommand<User>;
```

**4. Missing Generated Files**

```bash
# Check generator is referenced correctly
dotnet list package | grep SourceGenerators

# Force regeneration
dotnet clean && dotnet build

# Check for analyzer configuration
ls analyzers.globalconfig 2>/dev/null || echo "No global config found"
```

**5. Handler Not Found in DI**

```csharp
// Generated handlers are static classes, not services
// ❌ Don't try to inject handlers
public class BadService
{
    public BadService(CreateUserCommandHandler handler) { } // Won't work
}

// ✅ Use message bus or call handlers directly
public class GoodService
{
    private readonly IMessageBus _messageBus;
    private readonly DbDataSource _dataSource;

    public async Task<int> CreateUser(string name)
    {
        var command = new CreateUserCommand(0, name);

        // Option 1: Via message bus
        return await _messageBus.InvokeAsync(command);

        // Option 2: Direct handler call
        return await CreateUserCommandHandler.HandleAsync(command, _dataSource);
    }
}
```

### Performance Considerations

**1. Parameter Object Creation**

```csharp
// Default case: No object allocation
[DbCommand(sp: "simple_proc")]
public partial record SimpleCommand(int Id, string Name) : ICommand<int>;
// Generated: return this; (no allocation)

// Snake case: Object allocation for parameter mapping
[DbCommand(sp: "simple_proc", paramsCase: DbParamsCase.SnakeCase)]
public partial record SimpleCommand(int Id, string Name) : ICommand<int>;
// Generated: return new { id = this.Id, name = this.Name }; (allocates anonymous object)
```

**2. Connection Management**

```csharp
// Generated handlers properly manage connections
public static async Task<int> HandleAsync(...)
{
    await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
    // Connection is properly disposed
}
```

**3. Command Reuse**

```csharp
// Command records are immutable and safe to reuse
var getUserQuery = new GetUserByIdQuery(123);

// Can be called multiple times safely
var user1 = await GetUserByIdQueryHandler.HandleAsync(getUserQuery, dataSource);
var user2 = await GetUserByIdQueryHandler.HandleAsync(getUserQuery, dataSource);
```

## Migration & Upgrade Guide

### From Manual Dapper Code

**Before (Manual Implementation):**

```csharp
public class UserRepository
{
    private readonly IDbConnection _connection;

    public async Task<User> GetByIdAsync(int userId)
    {
        const string sql = "SELECT * FROM users WHERE id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = userId });
    }

    public async Task<int> CreateAsync(string name)
    {
        const string sql = "INSERT INTO users (name) VALUES (@Name) RETURNING id";
        return await _connection.ExecuteScalarAsync<int>(sql, new { Name = name });
    }
}
```

**After (Generated Implementation):**

```csharp
// Define commands/queries
[DbCommand(sql: "SELECT * FROM users WHERE id = @Id")]
public partial record GetUserByIdQuery(int Id) : ICommand<User>;

[DbCommand(sql: "INSERT INTO users (name) VALUES (@Name) RETURNING id")]
public partial record CreateUserCommand(string Name) : ICommand<int>;

// Repository uses generated handlers
public class UserRepository
{
    private readonly DbDataSource _dataSource;

    public UserRepository(DbDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<User> GetByIdAsync(int userId)
    {
        var query = new GetUserByIdQuery(userId);
        return await GetUserByIdQueryHandler.HandleAsync(query, _dataSource);
    }

    public async Task<int> CreateAsync(string name)
    {
        var command = new CreateUserCommand(name);
        return await CreateUserCommandHandler.HandleAsync(command, _dataSource);
    }
}
```

**Migration Benefits:**

-   **Type Safety**: Compile-time validation of parameters
-   **Consistency**: Standardized patterns across all data access
-   **Maintainability**: Changes to commands automatically update all usage
-   **Testing**: Commands are value objects, easy to test
-   **Performance**: Generated code is optimized and allocation-efficient

## Real-World Examples

### E-Commerce Order Management

```csharp
// Order creation with inventory check
[DbCommand(sp: "orders_create_with_inventory_check", nonQuery: true)]
public partial record CreateOrderCommand(
    Guid CustomerId,
    [Column("product_id")] Guid ProductId,
    int Quantity,
    decimal UnitPrice
) : ICommand<int>;

// Order status updates
[DbCommand(sql: "UPDATE orders SET status = @Status, updated_at = NOW() WHERE id = @OrderId")]
public partial record UpdateOrderStatusCommand(Guid OrderId, string Status) : ICommand<int>;

// Order queries with joins
[DbCommand(fn: "select * from orders_get_with_customer_details", paramsCase: DbParamsCase.SnakeCase)]
public partial record GetOrdersWithCustomerQuery(
    int Limit,
    int Offset,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate
) : IQuery<IEnumerable<OrderWithCustomer>>;

public record OrderWithCustomer(
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt
);
```

### User Authentication & Authorization

```csharp
// User authentication
[DbCommand(sql: "SELECT id, email, password_hash, is_active FROM users WHERE email = @Email AND is_active = true")]
public partial record GetUserByEmailQuery(string Email) : IQuery<UserCredentials>;

// Password updates
[DbCommand(sp: "users_update_password", nonQuery: true)]
public partial record UpdateUserPasswordCommand(
    Guid UserId,
    string PasswordHash,
    DateTime UpdatedAt
) : ICommand<int>;

// Role assignments
[DbCommand(sql: "INSERT INTO user_roles (user_id, role_id) VALUES (@UserId, @RoleId) ON CONFLICT DO NOTHING")]
public partial record AssignUserRoleCommand(Guid UserId, Guid RoleId) : ICommand<int>;

// User permissions query
[DbCommand(fn: "select * from users_get_permissions")]
public partial record GetUserPermissionsQuery(Guid UserId) : IQuery<IEnumerable<string>>;

public record UserCredentials(Guid Id, string Email, string PasswordHash, bool IsActive);
```

### Reporting & Analytics

```csharp
// Daily sales report
[DbCommand(fn: "select * from reports_daily_sales", paramsCase: DbParamsCase.SnakeCase)]
public partial record GetDailySalesReportQuery(
    DateTime StartDate,
    DateTime EndDate,
    string? ProductCategory
) : IQuery<IEnumerable<DailySalesData>>;

// Customer analytics
[DbCommand(sql: """
    SELECT
        customer_id,
        COUNT(*) as order_count,
        SUM(total_amount) as total_spent,
        AVG(total_amount) as avg_order_value,
        MAX(created_at) as last_order_date
    FROM orders
    WHERE created_at >= @FromDate
        AND (@CustomerId IS NULL OR customer_id = @CustomerId)
    GROUP BY customer_id
    ORDER BY total_spent DESC
    """)]
public partial record GetCustomerAnalyticsQuery(
    DateTime FromDate,
    Guid? CustomerId
) : IQuery<IEnumerable<CustomerAnalytics>>;

public record DailySalesData(DateTime Date, decimal TotalRevenue, int OrderCount);
public record CustomerAnalytics(Guid CustomerId, int OrderCount, decimal TotalSpent, decimal AvgOrderValue, DateTime LastOrderDate);
```

## Related Packages

-   **[Momentum.Extensions.Abstractions](../Momentum.Extensions.Abstractions/README.md)** - Core abstractions and attributes (required)
-   **[Momentum.Extensions](../Momentum.Extensions/README.md)** - Runtime utilities and helpers
-   **[Momentum.ServiceDefaults](../Momentum.ServiceDefaults/README.md)** - Service configuration and DI setup

## Package Information

-   **Target Framework**: .NET Standard 2.1 (Generator Host)
-   **Generated Code Target**: Any .NET version supporting Dapper
-   **Roslyn Version**: 4.0+
-   **Dependencies**: Momentum.Extensions.Abstractions, Dapper
-   **Package Type**: DevelopmentDependency (Analyzer package)

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/vgmello/momentum-sample/blob/main/LICENSE) file for details.
