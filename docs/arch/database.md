# Database Schema

The AppDomain service uses PostgreSQL with a well-defined schema managed through Liquibase migrations.

## Database Project Structure

The database is organized using Liquibase migrations with a single file per object pattern, grouped by domain and object type:

```
infra/AppDomain.Database/Liquibase/
├── billing/
│   ├── cashiers/
│   │   ├── tables/       # Cashier-related tables
│   │   │   ├── cashiers.sql
│   │   │   └── cashier_currencies.sql
│   │   └── procedures/   # Cashier-related procedures
│   │       └── cashiers_get_all.sql
│   ├── invoices/
│   │   ├── tables/       # Invoice-related tables
│   │   │   └── invoices.sql
│   │   └── procedures/   # Invoice-related procedures
│   │       ├── invoices_cancel.sql
│   │       └── invoices_mark_paid.sql
│   └── billing.sql    # Billing domain schema setup
└── service_bus/
    └── service_bus.sql   # Messaging infrastructure
```

### Naming Conventions

**Stored Procedures**: Follow the pattern `{subdomain}_{action}` where subdomain represents the business domain

-   Examples: `cashiers_get`, `cashiers_get_all`, `cashiers_create`, `invoices_get`, `invoices_create`
-   This ensures clear namespace separation and avoids conflicts

**Tables**: Use singular entity names within appropriate schemas

-   Domain tables: `AppDomain.{entity}` (e.g., `main.cashiers`, `main.invoices`)
-   Infrastructure tables: `service_bus.{entity}` (e.g., `service_bus.outbox`)

### Migration Configuration Files

-   `liquibase.properties` - Main domain schema migrations
-   `liquibase.servicebus.properties` - Service bus schema migrations
-   `liquibase.setup.properties` - Database setup and initialization

## Database Tables

| Schema      | Table              | Purpose                                         |
| ----------- | ------------------ | ----------------------------------------------- |
| AppDomain   | cashiers           | Primary table for cashier management            |
| AppDomain   | cashier_currencies | Multi-currency support for cashiers             |
| AppDomain   | invoices           | Invoice information and status tracking         |
| service_bus | outbox             | Outbox pattern for reliable message publishing  |
| service_bus | inbox              | Inbox pattern for idempotent message processing |

## Stored Procedures

| Function            | Purpose                            |
| ------------------- | ---------------------------------- |
| cashiers_get        | Get single cashier by ID           |
| cashiers_get_all    | Get paginated list of cashiers     |
| cashiers_create     | Create new cashier with validation |
| cashiers_update     | Update existing cashier            |
| cashiers_delete     | Soft delete cashier                |
| invoices_get        | Get paginated list of invoices     |
| invoices_get_single | Get single invoice by ID           |
| invoices_create     | Create new invoice                 |
| invoices_mark_paid  | Mark invoice as paid               |
| invoices_cancel     | Cancel invoice                     |

## Data Access Patterns

### Source Generator Integration

The service uses source generators for type-safe database operations with stored procedures:

```csharp
[DbCommand(fn: "select * from main.cashiers_get")]
public partial record GetCashierDbQuery(Guid CashierId) : IQuery<Cashier?>;
```

This generates:

-   Parameter binding methods
-   Result mapping logic
-   Compile-time validation
-   Type-safe stored procedure calls

## Migration Management

### Single File Per Object Pattern

Each database object (table, procedure, function, etc.) is defined in its own SQL file using Liquibase's formatted SQL syntax. This approach provides better organization, version control, and maintainability.

### File Structure and Naming

- **Tables**: Located in `{domain}/{subdomain}/tables/{object_name}.sql`
- **Procedures**: Located in `{domain}/{subdomain}/procedures/{procedure_name}.sql`
- **Schema Setup**: Domain-level files like `billing.sql` for schema initialization

### Example Migration File

```sql
--liquibase formatted sql
--changeset dev_user:"create cashiers table"
CREATE TABLE IF NOT EXISTS main.cashiers (
    tenant_id UUID,
    cashier_id UUID,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100),
    created_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    updated_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    PRIMARY KEY (tenant_id, cashier_id)
);

--changeset dev_user:"add email to cashiers table"
ALTER TABLE main.cashiers
ADD COLUMN IF NOT EXISTS email VARCHAR(100);
```

### Migration Benefits

- **Single Responsibility**: Each file manages one database object
- **Clear History**: Changes to specific objects are easily tracked
- **Domain Organization**: Related objects are grouped by business domain
- **Version Control Friendly**: Smaller files reduce merge conflicts

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
