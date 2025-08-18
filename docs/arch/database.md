# Database Schema

The AppDomain service uses PostgreSQL with a well-defined schema managed through Liquibase migrations.

## Database Project Structure

The database is organized using Liquibase migrations with a clear separation of concerns:

```
infra/AppDomain.Database/Liquibase/
├── AppDomain/
│   ├── tables/           # Domain tables
│   └── procedures/       # Domain stored procedures
└── service_bus/          # Messaging infrastructure
```

### Naming Conventions

**Stored Procedures**: Follow the pattern `{subdomain}_{action}` where subdomain represents the business domain

-   Examples: `cashiers_get`, `cashiers_get_all`, `cashiers_create`, `invoices_get`, `invoices_create`
-   This ensures clear namespace separation and avoids conflicts

**Tables**: Use singular entity names within appropriate schemas

-   Domain tables: `AppDomain.{entity}` (e.g., `AppDomain.cashiers`, `AppDomain.invoices`)
-   Infrastructure tables: `service_bus.{entity}` (e.g., `service_bus.outbox`)

### Migration Configuration Files

-   `liquibase.properties` - Main domain schema migrations
-   `liquibase.servicebus.properties` - Service bus schema migrations
-   `liquibase.setup.properties` - Database setup and initialization

## Database Tables

| Schema      | Table              | Purpose                                         | Definition                                                                                           |
| ----------- | ------------------ | ----------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| AppDomain   | cashiers           | Primary table for cashier management            | [cashiers.sql](infra/AppDomain.Database/Liquibase/AppDomain/tables/cashiers.sql)                     |
| AppDomain   | cashier_currencies | Multi-currency support for cashiers             | [cashier_currencies.sql](infra/AppDomain.Database/Liquibase/AppDomain/tables/cashier_currencies.sql) |
| AppDomain   | invoices           | Invoice information and status tracking         | [invoices.sql](infra/AppDomain.Database/Liquibase/AppDomain/tables/invoices.sql)                     |
| service_bus | outbox             | Outbox pattern for reliable message publishing  | [service_bus.sql](infra/AppDomain.Database/Liquibase/service_bus/service_bus.sql)                    |
| service_bus | inbox              | Inbox pattern for idempotent message processing | [service_bus.sql](infra/AppDomain.Database/Liquibase/service_bus/service_bus.sql)                    |

## Stored Procedures

| Function            | Purpose                            | Definition                                                                                                 |
| ------------------- | ---------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| cashiers_get        | Get single cashier by ID           | [cashiers_get.sql](infra/AppDomain.Database/Liquibase/AppDomain/procedures/cashier_get.sql)                |
| cashiers_get_all    | Get paginated list of cashiers     | [cashiers_get_all.sql](infra/AppDomain.Database/Liquibase/AppDomain/procedures/cashiers_get.sql)           |
| cashiers_create     | Create new cashier with validation | [cashiers.sql](infra/AppDomain.Database/Liquibase/AppDomain/procedures/cashiers_create.sql)                |
| cashiers_update     | Update existing cashier            | [cashiers_update.sql](infra/AppDomain.Database/Liquibase/AppDomain/procedures/cashiers_update.sql)         |
| cashiers_delete     | Soft delete cashier                | [cashiers_delete.sql](infra/AppDomain.Database/Liquibase/AppDomain/procedures/cashiers_delete.sql)         |
| invoices_get        | Get paginated list of invoices     | [invoices_get.sql](infra/AppDomain.Database/Liquibase/AppDomain/procedures/invoices_get.sql)               |
| invoices_get_single | Get single invoice by ID           | [invoices_get_single.sql](infra/AppDomain.Database/Liquibase/AppDomain/procedures/invoices_get_single.sql) |
| invoices_create     | Create new invoice                 | [invoices_create.sql](infra/AppDomain.Database/Liquibase/AppDomain/procedures/invoices_create.sql)         |
| invoices_mark_paid  | Mark invoice as paid               | [invoices_mark_paid.sql](infra/AppDomain.Database/Liquibase/AppDomain/procedures/invoices_mark_paid.sql)   |
| invoices_cancel     | Cancel invoice                     | [invoices.sql](infra/AppDomain.Database/Liquibase/AppDomain/procedures/invoices_cancel.sql)                |

## Data Access Patterns

### Source Generator Integration

The service uses source generators for type-safe database operations with stored procedures:

```csharp
[DbCommand(fn: "select * from AppDomain.cashiers_get")]
public partial record GetCashierDbQuery(Guid CashierId) : IQuery<Cashier?>;
```

This generates:

-   Parameter binding methods
-   Result mapping logic
-   Compile-time validation
-   Type-safe stored procedure calls

## Migration Management

### Liquibase Changelog Structure

```xml
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog xmlns="http://www.liquibase.org/xml/ns/dbchangelog">

    <!-- Include domain-specific changelogs -->
    <include file="AppDomain/changelog.xml" relativeToChangelogFile="true"/>
    <include file="service_bus/changelog.xml" relativeToChangelogFile="true"/>

</databaseChangeLog>
```

### Domain Changelog Example

```xml
<databaseChangeLog xmlns="http://www.liquibase.org/xml/ns/dbchangelog">

    <changeSet id="001-create-cashiers-table" author="AppDomain-team">
        <sqlFile path="tables/cashiers.sql" relativeToChangelogFile="true"/>
    </changeSet>

    <changeSet id="002-create-cashier-currencies-table" author="AppDomain-team">
        <sqlFile path="tables/cashier_currencies.sql" relativeToChangelogFile="true"/>
    </changeSet>

    <changeSet id="003-create-cashier-procedure" author="AppDomain-team">
        <sqlFile path="procedures/create_cashier.sql" relativeToChangelogFile="true"/>
    </changeSet>

</databaseChangeLog>
```

### Running Migrations

```bash
# Check current database status
liquibase status

# Apply all pending migrations
liquibase update

# Rollback last migration
liquibase rollback-count 1

# View migration history
liquibase history
```

## Performance Considerations

### Indexing Strategy

-   Primary keys on all tables for fast lookups
-   Foreign key indexes for join performance
-   Composite indexes for common query patterns
-   GIN indexes for JSONB metadata searches

### Partitioning

For high-volume tables like invoices, consider partitioning by:

-   Date ranges (monthly/quarterly partitions)
-   Status values (active vs. archived)
-   Currency codes for multi-tenant scenarios

### Connection Pooling

-   Use connection pooling for optimal performance
-   Configure appropriate pool sizes based on load
-   Monitor connection usage and adjust as needed

This database schema provides a solid foundation for the AppDomain service with proper normalization, indexing, and migration management.
