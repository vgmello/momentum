---
name: create-migration
description: Create a new Liquibase database migration following Momentum conventions
user-invocable: true
---

# Create Liquibase Migration

Create a new Liquibase migration file following the Momentum database conventions.

## Arguments

The user should provide:
- **domain**: The domain name (e.g., `invoices`, `cashiers`, `payments`)
- **type**: One of `table`, `procedure`, `alter` (default: `table`)
- **name**: The specific name (e.g., table name, procedure name)

## Conventions

### Directory Structure

All migrations live under `infra/AppDomain.Database/Liquibase/app_domain/` organized by domain:

```
app_domain/{domain}/
├── tables/
│   └── {table_name}.sql          # Table + constraints + indexes + triggers
└── procedures/
    └── {procedure_name}.sql      # Stored procedures/functions
```

### Changeset Format

Every SQL file uses Liquibase formatted SQL with changesets:

```sql
--liquibase formatted sql
--changeset dev_user:"{description}"
```

For stored procedures that should be rerunnable:
```sql
--changeset dev_user:"{description}" runOnChange:true splitStatements:false
```

### Table Files

Tables include ALL related constraints, indexes, and triggers in the SAME file. Never create separate constraint files.

```sql
--liquibase formatted sql
--changeset dev_user:"create {table_name} table"
CREATE TABLE IF NOT EXISTS main.{table_name} (
    tenant_id UUID,
    {entity}_id UUID,
    -- columns here
    created_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    updated_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    PRIMARY KEY (tenant_id, {entity}_id)
);

--changeset dev_user:"add performance indexes to {table_name} table"
CREATE INDEX IF NOT EXISTS idx_{table_name}_tenant
ON main.{table_name}(tenant_id);
```

Key rules:
- Schema is always `main`
- Primary keys use composite `(tenant_id, {entity}_id)`
- Always include `created_date_utc` and `updated_date_utc` with UTC defaults
- Use `IF NOT EXISTS` / `IF EXISTS` for idempotency
- Use `snake_case` for all database identifiers
- Index naming: `idx_{table}_{columns}`
- Unique index naming: `idx_{table}_unique_{columns}`

### Stored Procedures

```sql
--liquibase formatted sql
--changeset dev_user:"create {procedure_name} function" runOnChange:true splitStatements:false
CREATE OR REPLACE FUNCTION main.{procedure_name}(
        IN p_tenant_id uuid,
        -- parameters here
    ) RETURNS SETOF main.{table_name} LANGUAGE SQL AS $$
SELECT *
FROM main.{table_name} t
WHERE t.tenant_id = p_tenant_id
-- query logic here
$$;
```

Key rules:
- Functions use `RETURNS SETOF main.{table}` for queries
- Use `LANGUAGE SQL` for simple queries, `LANGUAGE plpgsql` for complex logic
- Prefix input parameters with `p_` when they conflict with column names
- Use `runOnChange:true splitStatements:false` for rerunnable functions

### Changelog Registration

After creating migration files, add them to `infra/AppDomain.Database/Liquibase/app_domain/changelog.xml`:

```xml
<include file="app_domain/{domain}/tables/{table_name}.sql"/>
<include file="app_domain/{domain}/procedures/{procedure_name}.sql"/>
```

## Workflow

1. Determine the domain and migration type from user input
2. Create the directory structure if needed (`tables/` or `procedures/`)
3. Create the SQL migration file following the conventions above
4. Add the `<include>` entry to `app_domain/changelog.xml`
5. Show the user what was created and remind them to run migrations:
   ```bash
   docker-compose --profile db up app-domain-db-migrations
   ```
