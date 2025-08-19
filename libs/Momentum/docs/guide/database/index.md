---
title: Database Integration
description: Multi-tenant database patterns, source generation, and PostgreSQL optimization strategies for Momentum applications with tenant-isolated data access.
date: 2024-01-15
---

# Database Integration

Multi-tenant database patterns, source generation, and PostgreSQL optimization strategies for Momentum applications with tenant-isolated data access.

## Overview

Momentum provides opinionated database access patterns specifically designed for multi-tenant SaaS applications with strict tenant isolation:

- **Multi-Tenant Architecture**: Composite primary keys with mandatory tenant isolation
- **DbCommand Source Generation**: Compile-time data access code generation for PostgreSQL functions
- **LinqToDB Entity Mapping**: High-performance object-relational mapping with attributes
- **Function-Based Data Access**: PostgreSQL functions with `$` prefix convention
- **Liquibase Migrations**: Version-controlled schema management with tenant awareness
- **Performance Optimization**: Tenant-aware indexing and query optimization

## Core Components

### DbCommand Generation
[DbCommand](./dbcommand) provides source-generated data access code with minimal runtime overhead and compile-time safety.

### Entity Mapping
[Entity Mapping](./entity-mapping) handles object-relational mapping with performance-optimized patterns and minimal configuration.

### Transaction Management
[Transactions](./transactions) ensures data consistency with proper transaction boundaries and error handling.

### Database Migrations
Database schema versioning and migration management using Liquibase for reliable deployments.

## Key Features

- **Source Generation**: Compile-time code generation for data access
- **Dapper Integration**: High-performance micro-ORM
- **PostgreSQL Optimization**: Database-specific optimizations
- **Connection Management**: Efficient connection pooling and lifecycle
- **Query Optimization**: Performance monitoring and optimization tools
- **Schema Migrations**: Automated database versioning

## Multi-Tenant Architecture

### Composite Primary Keys
All entities in Momentum use composite primary keys with `TenantId` as the first component, ensuring complete tenant isolation:

```csharp
[Table(Schema = "app_domain", Name = "cashiers")]
public record Cashier : DbEntity
{
    [PrimaryKey(Order = 0)]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [PrimaryKey(Order = 1)]
    [Column("cashier_id")]
    public Guid CashierId { get; set; }
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("email")]
    public string? Email { get; set; }
}
```

### DbEntity Base Class
Provides common auditing fields and optimistic concurrency control:

```csharp
[Table(Schema = "app_domain")]
public abstract record DbEntity
{
    [Column("created_date_utc", SkipOnUpdate = true)]
    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;
    
    [Column("updated_date_utc")]
    public DateTime UpdatedDateUtc { get; set; } = DateTime.UtcNow;
    
    [Column("xmin", SkipOnInsert = true, SkipOnUpdate = true)]
    [OptimisticLockProperty(VersionBehavior.Auto)]
    public int Version { get; init; } = 0;
}
```

## Data Access Patterns

### Function-Based Query Pattern
Use PostgreSQL functions with the `$` prefix for automatic `SELECT * FROM` generation:

```csharp
// Handler with source-generated database access
public static partial class GetCashiersQueryHandler
{
    [DbCommand(fn: "$app_domain.cashiers_get_all")]
    public partial record DbQuery(Guid TenantId, int Limit, int Offset) 
        : IQuery<IEnumerable<Data.Entities.Cashier>>;
        
    public static async Task<IEnumerable<GetCashiersQuery.Result>> Handle(
        GetCashiersQuery query, 
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbQuery = new DbQuery(query.TenantId, query.Limit, query.Offset);
        var cashiers = await messaging.InvokeQueryAsync(dbQuery, cancellationToken);
        
        return cashiers.Select(c => new GetCashiersQuery.Result(
            c.TenantId, c.CashierId, c.Name, c.Email ?? "N/A"));
    }
}
```

### Direct SQL Pattern
For simple queries that don't require custom functions:

```csharp
[DbCommand(sql: "SELECT * FROM app_domain.orders WHERE tenant_id = @TenantId AND order_id = @OrderId")]
public partial record GetOrderQuery(Guid TenantId, Guid OrderId) : IQuery<Order?>;
```

### Stored Procedure Pattern
For complex operations requiring transaction management:

```csharp
[DbCommand(sp: "app_domain.orders_process_payment")]
public partial record ProcessPaymentCommand(Guid TenantId, Guid OrderId, decimal Amount) 
    : ICommand<Result<PaymentResult>>;
```

## Database Technologies

### PostgreSQL
Primary database with advanced features:
- JSONB support for document storage
- Advanced indexing strategies
- Full-text search capabilities
- Partition management
- Connection pooling with Npgsql

### Liquibase
Schema migration management with multi-tenant awareness:
- Version-controlled database changes with composite key patterns
- Tenant-aware table structures and constraints
- Multi-tenant indexing strategies
- Environment-specific migrations with tenant isolation
- Automated deployment integration

## Performance Features

### Multi-Tenant Query Optimization
- **Tenant-First Indexing**: All indexes include `tenant_id` as the first column for optimal query performance
- **Composite Key Performance**: Leverages PostgreSQL's efficient composite key handling
- **Function-Based Access**: Uses PostgreSQL functions to eliminate N+1 query problems
- **Source Generation**: Compile-time query generation eliminates runtime reflection overhead

### Index Strategy for Multi-Tenancy
```sql
-- Primary composite index (automatic with PRIMARY KEY)
PRIMARY KEY (tenant_id, entity_id)

-- Secondary indexes always start with tenant_id
CREATE INDEX idx_cashiers_tenant_name ON app_domain.cashiers(tenant_id, name);
CREATE INDEX idx_cashiers_tenant_email ON app_domain.cashiers(tenant_id, email) WHERE email IS NOT NULL;

-- Filtered indexes for common queries
CREATE INDEX idx_active_orders ON app_domain.orders(tenant_id, status) WHERE status IN ('pending', 'processing');
```

### Query Performance Best Practices
```csharp
// ✅ Correct: Always filter by tenant first
WHERE tenant_id = @TenantId AND cashier_id = @CashierId

// ✅ Correct: Use composite key in WHERE clause
WHERE (tenant_id, cashier_id) = (@TenantId, @CashierId)

// ❌ Incorrect: Missing tenant_id filter
WHERE cashier_id = @CashierId

// ❌ Incorrect: Wrong index order
WHERE cashier_id = @CashierId AND tenant_id = @TenantId
```

### Connection Management
- **Npgsql Connection Pooling**: Optimized for high-concurrency multi-tenant scenarios
- **Per-Tenant Connection Limits**: Prevents tenant isolation violations
- **Connection String Security**: Encrypted credentials with proper access controls

## Configuration

### Connection Strings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=myapp;Username=user;Password=pass"
  }
}
```

### DbCommand Settings
```json
{
  "DbCommand": {
    "DefaultParamCase": "SnakeCase",
    "ParamPrefix": "@",
    "Timeout": 30
  },
  "Logging": {
    "LogLevel": {
      "Momentum.Extensions.Database": "Information",
      "Npgsql": "Warning"
    }
  }
}
```

## Entity Mapping with LinqToDB

### Entity Attributes
Momentum uses LinqToDB attributes for precise database mapping:

```csharp
[Table(Schema = "app_domain", Name = "cashiers")]
public record Cashier : DbEntity
{
    // Composite primary key with explicit ordering
    [PrimaryKey(Order = 0)]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [PrimaryKey(Order = 1)]
    [Column("cashier_id")]
    public Guid CashierId { get; set; }
    
    // Column mapping with explicit names
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    // Nullable columns
    [Column("email")]
    public string? Email { get; set; }
}
```

### Mapperly Integration
Domain model mapping using Mapperly source generation:

```csharp
[Mapper]
public static partial class DbMapper
{
    [MapperIgnoreSource(nameof(Entities.Cashier.CreatedDateUtc))]
    [MapperIgnoreSource(nameof(Entities.Cashier.UpdatedDateUtc))]
    [MapperIgnoreTarget(nameof(Cashier.CashierPayments))]
    public static partial Cashier ToModel(this Entities.Cashier cashier);
}
```

### Optimistic Concurrency
Using PostgreSQL's `xmin` system column for optimistic locking:

```csharp
[Column("xmin", SkipOnInsert = true, SkipOnUpdate = true)]
[OptimisticLockProperty(VersionBehavior.Auto)]
public int Version { get; init; } = 0;
```

## Migration Patterns

### Multi-Tenant Table Structure
Tables with composite primary keys for tenant isolation:

```sql
-- Multi-tenant table with composite primary key
CREATE TABLE app_domain.cashiers (
    tenant_id UUID,
    cashier_id UUID,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100),
    created_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    updated_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    PRIMARY KEY (tenant_id, cashier_id)
);

-- Tenant-aware indexes for performance
CREATE INDEX idx_cashiers_tenant_name ON app_domain.cashiers(tenant_id, name);
CREATE INDEX idx_cashiers_tenant_email ON app_domain.cashiers(tenant_id, email) WHERE email IS NOT NULL;
```

### PostgreSQL Functions for Data Access
Functions that enforce tenant isolation:

```sql
CREATE OR REPLACE FUNCTION app_domain.cashiers_get_all(
    IN p_tenant_id uuid,
    IN p_limit integer DEFAULT 1000,
    IN p_offset integer DEFAULT 0
) RETURNS SETOF app_domain.cashiers LANGUAGE SQL AS $$
SELECT *
FROM app_domain.cashiers c
WHERE c.tenant_id = p_tenant_id
ORDER BY c.name
LIMIT p_limit OFFSET p_offset;
$$;
```

### Schema Management
- Liquibase changesets with tenant-aware patterns
- Composite key constraints and relationships
- Multi-tenant indexing strategies
- Function versioning with `runOnChange:true`

## Result<T> Pattern Integration

### Command Results with Database Operations
Commands use the Result<T> pattern for explicit error handling:

```csharp
public static async Task<Result<Order>> Handle(
    CreateOrderCommand command,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // Validation
    if (command.Items.Count == 0)
        return Result.Failure<Order>("Order must contain at least one item");
    
    // Database operation via source generation
    var dbCommand = new DbCommand(command.TenantId, command.CustomerId, itemsJson);
    var dbResult = await messaging.InvokeAsync(dbCommand, cancellationToken);
    
    if (dbResult.IsFailure)
        return Result.Failure<Order>(dbResult.Error);
    
    // Fetch created entity
    var orderQuery = new GetOrderQuery(command.TenantId, dbResult.Value);
    var order = await messaging.InvokeAsync(orderQuery, cancellationToken);
    
    return order is not null 
        ? Result.Success(order)
        : Result.Failure<Order>("Failed to retrieve created order");
}
```

### Query Results with Nullable Types
Queries return nullable types for single entities:

```csharp
public static async Task<Cashier?> Handle(
    GetCashierQuery query,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    var cashier = await db.Cashiers
        .FirstOrDefaultAsync(c => c.TenantId == query.TenantId && c.CashierId == query.CashierId, 
                           cancellationToken);
    
    return cashier?.ToModel();
}
```

## Error Handling

### Database-Level Error Handling
- **Tenant Isolation**: Composite key constraints prevent cross-tenant data access
- **Function Validation**: PostgreSQL functions validate business rules at database level
- **Optimistic Concurrency**: `xmin` column prevents concurrent update conflicts
- **Connection Resilience**: Automatic retry policies for transient failures

### Application-Level Error Handling
```csharp
// Database function error handling
CREATE OR REPLACE FUNCTION app_domain.orders_create(...)
RETURNS UUID AS $$
BEGIN
    -- Validation
    IF p_tenant_id IS NULL THEN
        RAISE EXCEPTION 'Tenant ID cannot be null';
    END IF;
    
    -- Business logic
    -- ...
    
    RETURN v_order_id;
EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Failed to create order: %', SQLERRM;
END;
$$ LANGUAGE plpgsql;
```

## Security

### Multi-Tenant Security
- **Composite Key Enforcement**: Database-level tenant isolation through primary key constraints
- **Function-Level Validation**: All database functions require and validate `tenant_id` parameters
- **Connection Security**: Encrypted connection strings with least-privilege database access
- **Row-Level Security**: PostgreSQL RLS policies can be implemented for additional tenant isolation

### SQL Injection Prevention
- **Parameterized Queries**: Source generation ensures all queries use proper parameterization
- **Function-Based Access**: PostgreSQL functions provide an additional abstraction layer
- **Type Safety**: Compile-time verification of parameter types and mappings

## Testing Strategies

### Multi-Tenant Testing Patterns
- **Tenant Isolation Tests**: Verify queries cannot access cross-tenant data
- **Composite Key Validation**: Ensure all operations include tenant context
- **Function Testing**: Unit test PostgreSQL functions with different tenant scenarios

### Integration Testing with Testcontainers
```csharp
public class DatabaseIntegrationTests : IntegrationTestFixture
{
    [Fact]
    public async Task GetCashiers_WithMultipleTenants_ReturnsOnlyTenantData()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        
        await SeedCashierAsync(tenant1, "Cashier 1A");
        await SeedCashierAsync(tenant1, "Cashier 1B");
        await SeedCashierAsync(tenant2, "Cashier 2A");
        
        // Act
        var query = new GetCashiersQuery(tenant1);
        var result = await SendAsync(query);
        
        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.TenantId == tenant1);
    }
}
```

### Database Schema Testing
- **Migration Testing**: Verify Liquibase migrations apply correctly
- **Constraint Testing**: Validate composite key and foreign key constraints
- **Performance Testing**: Measure query performance with tenant-aware indexes

## Best Practices

### Multi-Tenant Data Access
- **Always Include Tenant Context**: Every query and command must include `TenantId`
- **Composite Primary Keys**: Use `(tenant_id, entity_id)` pattern for all entities
- **Function-Based Access**: Prefer PostgreSQL functions over direct SQL for complex operations
- **Source Generation**: Use `[DbCommand]` attributes for compile-time safety

### Performance Optimization
- **Tenant-First Indexing**: Create indexes with `tenant_id` as the first column
- **Connection Pooling**: Configure Npgsql connection pooling appropriately
- **Query Monitoring**: Monitor slow queries and add indexes as needed
- **Pagination**: Always use `LIMIT`/`OFFSET` for queries returning multiple records

### Security and Isolation
- **Validate Tenant Context**: Database functions should validate tenant parameters
- **Prevent Cross-Tenant Access**: Use composite keys and proper WHERE clauses
- **Connection Security**: Use encrypted connection strings and proper credentials
- **Audit Trails**: Implement audit logging for sensitive operations

## Real-World Examples

The following examples are taken directly from the Momentum template's Cashiers and Invoices domains:

### Cashier Entity with Composite Key
```csharp
[Table(Schema = "app_domain", Name = "cashiers")]
public record Cashier : DbEntity
{
    [PrimaryKey(Order = 0)]
    public Guid TenantId { get; set; }

    [PrimaryKey(Order = 1)]
    public Guid CashierId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
}
```

### Function-Based Query Handler
```csharp
public static partial class GetCashiersQueryHandler
{
    [DbCommand(fn: "$app_domain.cashiers_get_all")]
    public partial record DbQuery(Guid TenantId, int Limit, int Offset) 
        : IQuery<IEnumerable<Data.Entities.Cashier>>;

    public static async Task<IEnumerable<GetCashiersQuery.Result>> Handle(
        GetCashiersQuery query, 
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbQuery = new DbQuery(query.TenantId, query.Limit, query.Offset);
        var cashiers = await messaging.InvokeQueryAsync(dbQuery, cancellationToken);

        return cashiers.Select(c => new GetCashiersQuery.Result(
            c.TenantId, c.CashierId, c.Name, c.Email ?? "N/A"));
    }
}
```

### PostgreSQL Function
```sql
CREATE OR REPLACE FUNCTION app_domain.cashiers_get_all(
    IN p_tenant_id uuid,
    IN p_limit integer DEFAULT 1000,
    IN p_offset integer DEFAULT 0
) RETURNS SETOF app_domain.cashiers LANGUAGE SQL AS $$
SELECT *
FROM app_domain.cashiers c
WHERE c.tenant_id = p_tenant_id
ORDER BY c.name
LIMIT p_limit OFFSET p_offset;
$$;
```

## Getting Started

1. **Define Entity Structure**: Create entities with composite primary keys using LinqToDB attributes
2. **Set up Liquibase Migrations**: Create tenant-aware table structures with proper indexing
3. **Create PostgreSQL Functions**: Implement business logic functions with tenant validation
4. **Define Query/Command Handlers**: Use `[DbCommand]` attributes for source generation
5. **Configure Connection Strings**: Set up secure PostgreSQL connections with proper pooling
6. **Implement Testing**: Add integration tests with Testcontainers for tenant isolation validation

## Related Topics

- [CQRS](../cqrs/)
- [Service Configuration](../service-configuration/)
- [Testing](../testing/)
- [Best Practices](../best-practices)