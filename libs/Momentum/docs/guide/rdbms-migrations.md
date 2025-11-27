---
title: Database Migrations with Liquibase
description: Comprehensive guide to database schema evolution and migrations in Momentum using Liquibase, including migration patterns, version control, deployment strategies, and team collaboration workflows.
date: 2024-01-15
---

# Database Migrations with Liquibase

Momentum uses Liquibase for database schema evolution, providing enterprise-grade migration management with support for multiple environments, rollbacks, and team collaboration. This guide covers migration patterns, deployment strategies, and best practices for maintaining database schemas in production applications.

> [!NOTE]
> **Template Examples Notice**: This documentation shows patterns using example domains like `cashiers` and `invoices` from the Momentum template. These are **not requirements** for your application. Replace these examples with your actual business domains (e.g., `orders`, `customers`, `products`, etc.). The `AppDomain` prefix is also a placeholder that gets replaced with your project name when using the template.

## Overview

Momentum's migration system focuses on:

-   **Version Control**: Track all schema changes with precise versioning
-   **Environment Safety**: Consistent deployments across development, staging, and production
-   **Zero-Downtime**: Migration patterns that minimize application downtime
-   **Team Collaboration**: Conflict-free workflows for distributed development
-   **Rollback Support**: Safe recovery from problematic migrations
-   **Audit Trail**: Complete history of all schema changes

## Architecture and Integration

### Project Structure

Momentum organizes database migrations in a dedicated infrastructure project that maintains clear separation between different database concerns:

```
infra/AppDomain.Database/
├── AppDomain.Database.csproj          # Minimal project file for build integration
├── liquibase.properties               # Main main database configuration
├── liquibase.servicebus.properties    # Service bus database configuration
├── liquibase.setup.properties         # Database setup and initial schemas
└── Liquibase/                         # Migration files directory
    ├── changelog.xml                  # Root changelog with includeAll directive
    ├── main/                    # Application domain migrations
    │   ├── changelog.xml              # Domain-specific changelog
    │   ├── main.sql             # Schema initialization
    │   ├── cashiers/                  # Domain entity migrations
    │   │   ├── tables/                # Table definitions and modifications
    │   │   │   ├── cashiers.sql
    │   │   │   └── cashier_currencies.sql
    │   │   └── procedures/            # Stored procedures and functions
    │   │       └── cashiers_get_all.sql
    │   └── invoices/                  # Another domain entity
    │       ├── tables/
    │       │   └── invoices.sql
    │       └── procedures/
    │           ├── invoices_cancel.sql
    │           └── invoices_mark_paid.sql
    └── service_bus/                   # Message infrastructure migrations
        ├── changelog.xml              # Service bus changelog
        └── service_bus.sql            # Message queues and schemas
```

### Multi-Database Architecture

Momentum uses a **dual-database approach** to separate concerns:

#### 1. **main Database**
- Contains business domain entities and logic
- Organized by domain boundaries (cashiers, invoices, etc.)
- Supports complex business queries and transactions
- Configured via `liquibase.properties`

#### 2. **service_bus Database**
- Manages message queues and event sourcing infrastructure
- Contains schemas for asynchronous processing
- Supports reliable message delivery patterns
- Configured via `liquibase.servicebus.properties`

### Migration Project Configuration

The database project uses a minimal **NoTargets SDK** configuration that allows it to participate in the solution build process without compiling executable code:

```xml
<Project>
    <PropertyGroup>
        <TargetFramework>netstandard2.0</TargetFramework>
        <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
        <ImportProjectExtensionProps>false</ImportProjectExtensionProps>
        <ImportProjectExtensionTargets>false</ImportProjectExtensionTargets>
        <ImportNuGetBuildTasksPackTargetsFromSdk>false</ImportNuGetBuildTasksPackTargetsFromSdk>
        <NuGetPropsFile>false</NuGetPropsFile>
    </PropertyGroup>

    <Import Project="Sdk.props" Sdk="Microsoft.Build.NoTargets" Version="3.7.56" />
    <Import Project="Sdk.targets" Sdk="Microsoft.Build.NoTargets" Version="3.7.56"/>

    <Target Name="PrepareForBuild" />
</Project>
```

This configuration ensures that:
- The project appears in Visual Studio and solution builds
- No compilation occurs (migrations are text files)
- Build dependencies can reference the migration project
- CI/CD pipelines can include database changes in build validation

## Liquibase Patterns and Organization

### Changelog Structure

Momentum uses a **hierarchical changelog organization** that promotes maintainability and team collaboration:

#### Root Changelog (`Liquibase/changelog.xml`)
```xml
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog
        https://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-latest.xsd">

    <includeAll path="" minDepth="2" relativeToChangelogFile="true"
                endsWithFilter="changelog.xml"/>
</databaseChangeLog>
```

The `includeAll` directive automatically discovers and includes all `changelog.xml` files in subdirectories, enabling:
- **Automatic discovery** of new domain migrations
- **Consistent ordering** based on directory structure
- **Minimal maintenance** when adding new domains

#### Domain Changelog (`main/changelog.xml`)
```xml
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog
        https://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-latest.xsd">

    <!-- Schema initialization -->
    <include file="main/main.sql"/>

    <!-- Domain entity migrations -->
    <include file="main/cashiers/tables/cashiers.sql"/>
    <include file="main/cashiers/tables/cashier_currencies.sql"/>
    <include file="main/cashiers/procedures/cashiers_get_all.sql"/>

    <include file="main/invoices/tables/invoices.sql"/>
    <include file="main/invoices/procedures/invoices_cancel.sql"/>
    <include file="main/invoices/procedures/invoices_mark_paid.sql"/>
</databaseChangeLog>
```

### Migration File Patterns

#### Liquibase Formatted SQL
All migration files use **Liquibase formatted SQL** syntax with embedded changeset metadata:

```sql
--liquibase formatted sql

--changeset author:"changeset description" [attributes]
-- SQL statements here

--changeset author:"another change" [attributes]
-- More SQL statements
```

#### Schema Initialization Pattern
```sql
--liquibase formatted sql
--changeset dev_user:"create database" runInTransaction:false context:@setup
CREATE DATABASE main;

--changeset dev_user:"create main schema"
CREATE SCHEMA IF NOT EXISTS main;
```

Key attributes:
- `runInTransaction:false`: Required for database creation
- `context:@setup`: Used for initial environment setup

#### Table Creation Pattern
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

Best practices:
- **Always use `IF NOT EXISTS`** for idempotent operations
- **Separate logical changes** into distinct changesets
- **Use descriptive changeset descriptions**
- **Include UTC timestamps** for audit trails

#### Stored Procedure Pattern
```sql
--liquibase formatted sql
--changeset dev_user:"create cashiers_get_all function" runOnChange:true splitStatements:false
CREATE OR REPLACE FUNCTION main.cashiers_get_all(
        IN p_tenant_id uuid,
        IN p_limit integer DEFAULT 1000,
        IN p_offset integer DEFAULT 0
    ) RETURNS SETOF main.cashiers LANGUAGE SQL AS $$
SELECT *
FROM main.cashiers c
WHERE c.tenant_id = p_tenant_id
ORDER BY c.name
LIMIT p_limit OFFSET p_offset;
$$;
```

Key attributes for procedures:
- `runOnChange:true`: Re-execute when file content changes
- `splitStatements:false`: Treat entire block as single statement

### Directory Organization Strategy

#### Domain-Driven Structure
```
main/
├── [domain_name]/           # One directory per domain
│   ├── tables/             # Table definitions and schema changes
│   ├── procedures/         # Stored procedures and functions
│   ├── views/             # Database views (optional)
│   └── data/              # Reference data inserts (optional)
```

#### File Naming Conventions
- **Tables**: `[entity_name].sql` (e.g., `cashiers.sql`)
- **Procedures**: `[entity]_[action].sql` (e.g., `cashiers_get_all.sql`)
- **Schema changes**: Descriptive names (e.g., `add_email_index.sql`)

#### Versioning Strategy
- **No version numbers in filenames** - Liquibase tracks execution order
- **Chronological organization** within directories
- **Logical grouping** by entity or feature

## Aspire Integration

### Container-Based Migration Execution

Momentum leverages **.NET Aspire orchestration** to manage database migrations as containerized resources, providing consistent execution across development environments.

#### LiquibaseExtensions Implementation

The `LiquibaseExtensions.cs` class provides a fluent API for configuring Liquibase migrations within Aspire:

```csharp
public static class LiquibaseExtensions
{
    public static IResourceBuilder<ContainerResource> AddLiquibaseMigrations(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> dbServerResource,
        IResourceBuilder<ParameterResource> dbPassword)
    {
        return builder
            .AddContainer("liquibase", "liquibase/liquibase:4.32-alpine")
            .WithBindMount("../../infra/AppDomain.Database/Liquibase", "/liquibase/changelog")
            .WithEnvironment("LIQUIBASE_COMMAND_USERNAME", "postgres")
            .WithEnvironment("LIQUIBASE_COMMAND_PASSWORD", dbPassword)
            .WithEnvironment("LIQUIBASE_COMMAND_CHANGELOG_FILE", "changelog.xml")
            .WithEnvironment("LIQUIBASE_SEARCH_PATH", "/liquibase/changelog")
            .WaitFor(dbServerResource)
            .WithReference(dbServerResource)
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c",
                """
                liquibase --url=jdbc:postgresql://app-domain-db:5432/service_bus update --changelog-file=service_bus/changelog.xml && \
                liquibase --url=jdbc:postgresql://app-domain-db:5432/main update --changelog-file=main/changelog.xml
                """);
    }
}
```

#### AppHost Configuration

In the Aspire AppHost project (`Program.cs`):

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Database server configuration
var dbPassword = builder.AddParameter("db-password", secret: true);
var pgsql = builder.AddPostgres("app-domain-db", password: dbPassword, port: 54320)
    .WithDataVolume();

// Migration configuration
var migrations = builder.AddLiquibaseMigrations(pgsql, dbPassword);

// Application services depend on completed migrations
builder.AddProject<Projects.AppDomain_Api>("app-domain-api")
    .WithReference(pgsql)
    .WaitFor(migrations);
```

### Environment Variable Configuration

The Liquibase container receives configuration through environment variables:

| Variable | Purpose | Value |
|----------|---------|--------|
| `LIQUIBASE_COMMAND_USERNAME` | Database authentication | `postgres` |
| `LIQUIBASE_COMMAND_PASSWORD` | Database password | Aspire parameter |
| `LIQUIBASE_COMMAND_CHANGELOG_FILE` | Root changelog path | `changelog.xml` |
| `LIQUIBASE_SEARCH_PATH` | Migration files location | `/liquibase/changelog` |

### Volume Mounting Strategy

```csharp
.WithBindMount("../../infra/AppDomain.Database/Liquibase", "/liquibase/changelog")
```

This bind mount:
- **Maps local development files** into the container
- **Enables real-time changes** during development
- **Maintains file permissions** and directory structure
- **Supports cross-platform development** (Windows, macOS, Linux)

### Dependency Management and Sequencing

#### Wait Dependencies
```csharp
.WaitFor(dbServerResource)  // Wait for PostgreSQL to be ready
```

Application services wait for migrations:
```csharp
builder.AddProject<Projects.AppDomain_Api>("app-domain-api")
    .WaitFor(migrations);    // Wait for migrations to complete
```

#### Sequential Database Creation
The migration container executes databases in sequence:
1. **service_bus** database (messaging infrastructure)
2. **main** database (business domain)

This ensures proper dependency ordering and prevents connection conflicts.

## Docker Compose Integration

### Service Definition

For non-Aspire deployments, Momentum provides Docker Compose configuration:

```yaml
app-domain-db-migrations:
  image: liquibase/liquibase:4.32-alpine
  profiles: [ "db", "api", "backoffice" ]
  volumes:
    - ./infra/AppDomain.Database:/app
  depends_on:
    app-domain-db:
      condition: service_healthy
  working_dir: /app
  entrypoint: /bin/sh
  command:
    - -c
    - |
      echo 'Running database migrations...' && \
      liquibase update --defaults-file liquibase.setup.properties --url=jdbc:postgresql://app-domain-db:5432/postgres && \
      liquibase update --defaults-file liquibase.servicebus.properties --url=jdbc:postgresql://app-domain-db:5432/service_bus && \
      liquibase update --url=jdbc:postgresql://app-domain-db:5432/main && \
      echo 'Database migrations completed successfully!'
```

### Configuration Files

#### Main Database (`liquibase.properties`)
```properties
changeLogFile=main/changelog.xml
liquibase.searchPath=./Liquibase/
liquibase.command.url=jdbc:postgresql://localhost:5432/main
username=postgres
password=password@
```

#### Service Bus (`liquibase.servicebus.properties`)
```properties
changeLogFile=service_bus/changelog.xml
liquibase.searchPath=./Liquibase/
liquibase.command.url=jdbc:postgresql://localhost:5432/service_bus
username=postgres
password=password@
```

#### Setup Configuration (`liquibase.setup.properties`)
Used for initial database and schema creation in containerized environments.

## Migration Workflows

### Development Workflow

#### 1. Creating New Migrations

**Step 1: Determine Migration Scope**
```bash
# Identify the domain and type of change
# Domain: cashiers, invoices, orders, etc.
# Type: table, procedure, view, data
```

**Step 2: Create Migration File**
```bash
# Navigate to appropriate directory
cd infra/AppDomain.Database/Liquibase/main/[domain]/[type]/

# Create new migration file
touch [descriptive_name].sql
```

**Step 3: Write Migration Content**
```sql
--liquibase formatted sql
--changeset [your_username]:"descriptive change description"
-- Your SQL statements here
```

**Step 4: Update Domain Changelog**
```xml
<!-- Add to main/[domain]/changelog.xml or main changelog -->
<include file="main/[domain]/[type]/[your_file].sql"/>
```

#### 2. Testing Migrations Locally

**Using .NET Aspire:**
```bash
# Start the complete application stack
dotnet run --project src/AppDomain.AppHost

# Migrations run automatically before application services
# Check Aspire dashboard: https://localhost:18110
```

**Using Docker Compose:**
```bash
# Run migrations only
docker compose up app-domain-db-migrations

# Run database and migrations
docker compose up app-domain-db app-domain-db-migrations
```

**Direct Liquibase Execution:**
```bash
# Navigate to database project
cd infra/AppDomain.Database

# Run main database migrations
liquibase update

# Run service bus migrations
liquibase update --defaults-file=liquibase.servicebus.properties
```

#### 3. Validating Changes

**Check Migration Status:**
```bash
# View migration history
liquibase history

# Check current database state
liquibase status

# Validate changelog syntax
liquibase validate
```

**Database Verification:**
```sql
-- Connect to database and verify changes
\d main.your_table_name    -- Replace with your actual table name
\df main.your_function_name -- Replace with your actual function name
```

### Adding New Domain Entities

#### 1. Create Domain Directory Structure
```bash
mkdir -p infra/AppDomain.Database/Liquibase/main/[new_domain]/{tables,procedures}
```

#### 2. Create Domain Changelog
```xml
<!-- infra/AppDomain.Database/Liquibase/main/[new_domain]/changelog.xml -->
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog
        https://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-latest.xsd">

    <!-- Include all tables first -->
    <include file="main/[new_domain]/tables/[entity].sql"/>

    <!-- Then procedures -->
    <include file="main/[new_domain]/procedures/[entity]_get_all.sql"/>
</databaseChangeLog>
```

#### 3. Create Entity Table Migration
```sql
--liquibase formatted sql
--changeset dev_user:"create [entity] table"
CREATE TABLE IF NOT EXISTS main.[entity_plural] (
    tenant_id UUID NOT NULL,
    [entity]_id UUID NOT NULL,
    name VARCHAR(100) NOT NULL,
    created_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    updated_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    PRIMARY KEY (tenant_id, [entity]_id)
);
```

#### 4. Create Basic Procedures
```sql
--liquibase formatted sql
--changeset dev_user:"create [entity]_get_all function" runOnChange:true splitStatements:false
CREATE OR REPLACE FUNCTION main.[entity]_get_all(
        IN p_tenant_id uuid,
        IN p_limit integer DEFAULT 1000,
        IN p_offset integer DEFAULT 0
    ) RETURNS SETOF main.[entity_plural] LANGUAGE SQL AS $$
SELECT *
FROM main.[entity_plural] e
WHERE e.tenant_id = p_tenant_id
ORDER BY e.name
LIMIT p_limit OFFSET p_offset;
$$;
```

### Rollback Strategies

#### Automatic Rollbacks
```bash
# Rollback last changeset
liquibase rollback-count 1

# Rollback to specific tag
liquibase rollback [tag_name]

# Rollback to specific date
liquibase rollback-to-date 2024-01-15
```

#### Manual Rollback Preparation
```sql
--liquibase formatted sql
--changeset dev_user:"add email column" # Template example
ALTER TABLE main.customers ADD COLUMN email VARCHAR(100); # Replace 'cashiers' with your entity
--rollback ALTER TABLE main.customers DROP COLUMN email; # Corresponding rollback
```

Include rollback instructions for complex changes:
- Data migrations
- Structural changes
- Constraint modifications

## Best Practices

### Team Collaboration

#### 1. **Changeset Authorship**
- Use **consistent author identifiers** across the team
- Include **meaningful descriptions** that explain the business reason
- **Reference ticket numbers** or feature identifiers when applicable

```sql
--changeset john_doe:"add invoice status index for performance - JIRA-1234"
CREATE INDEX IF NOT EXISTS idx_invoices_status ON main.invoices(status);
```

#### 2. **Merge Conflict Prevention**
- **Organize by domain** to reduce conflicts between teams
- **Use timestamp-based organization** within domains when multiple developers work simultaneously
- **Communicate schema changes** early in team standups or planning

#### 3. **Code Review Process**
- **Review all migration files** before merging
- **Validate against multiple database versions** when possible
- **Test rollback procedures** for complex changes
- **Check for data migration performance** with large datasets

### Versioning and Deployment

#### 1. **Environment Consistency**
```bash
# Generate changeset checksums for validation
liquibase update-sql > pending_changes.sql

# Validate changelog before deployment
liquibase validate

# Check deployment status
liquibase status --verbose
```

#### 2. **Production Deployment Strategy**

**Blue-Green Deployments:**
- Run migrations on **green environment** first
- Validate application functionality
- Switch traffic after verification
- Keep **blue environment** for rapid rollback

**Rolling Deployments:**
- Design **backward-compatible** schema changes
- Use **multiple deployment phases** for breaking changes:
  1. Add new columns (optional)
  2. Deploy application code
  3. Remove old columns (separate release)

**Maintenance Windows:**
- Schedule **complex migrations** during low-traffic periods
- Use **locks and timeouts** to prevent concurrent access
- Monitor **migration duration** and database performance

#### 3. **Performance Considerations**

**Large Table Migrations:**
```sql
--changeset dev_user:"add index concurrently"
CREATE INDEX CONCURRENTLY idx_invoices_created_date
ON main.invoices(created_date_utc);
```

**Data Migrations:**
```sql
--changeset dev_user:"migrate legacy data in batches" splitStatements:false # Template pattern
DO $$
DECLARE
    batch_size INTEGER := 1000;
    total_rows INTEGER;
BEGIN
    -- Process in batches to avoid lock contention
    LOOP
        UPDATE main.your_table_name  -- Replace with your actual table
        SET new_column = legacy_column
        WHERE new_column IS NULL
        LIMIT batch_size;

        GET DIAGNOSTICS total_rows = ROW_COUNT;
        EXIT WHEN total_rows = 0;

        -- Brief pause between batches
        PERFORM pg_sleep(0.1);
    END LOOP;
END $$;
```

### Monitoring and Observability

#### 1. **Migration Tracking**
```sql
-- Check migration history
SELECT * FROM databasechangelog
ORDER BY dateexecuted DESC
LIMIT 10;

-- Verify checksums
SELECT id, author, filename, md5sum
FROM databasechangelog
WHERE md5sum IS NULL;
```

#### 2. **Performance Monitoring**
- **Track migration execution time** in CI/CD pipelines
- **Monitor database locks** during migration execution
- **Alert on failed migrations** in production environments
- **Log migration output** for troubleshooting

#### 3. **Audit and Compliance**
- **Retain migration logs** for compliance requirements
- **Document schema changes** for security audits
- **Track privilege changes** and access modifications
- **Maintain change approval records** for production deployments

## Troubleshooting

### Common Issues and Solutions

#### 1. **Changeset Checksum Mismatches**

**Problem:** Liquibase detects that a previously run changeset has been modified.

```
Validation Failed:
     1 changesets check sum
          main/customers/tables/customers.sql::dev_user::create customers table
```

> [!NOTE]
> This example shows the error format using `customers` instead of the template's `cashiers` entity.

**Solutions:**

**Option A: Clear Checksums (Development Only)**
```bash
# Clear checksums for specific changeset
liquibase clear-checksums

# Update checksums to current values
liquibase update
```

**Option B: Update Checksums (Production)**
```bash
# Mark specific changeset as run with new checksum (template example)
liquibase changeset-status --changeset-id="create customers table" \ # Replace with your entity
  --changeset-author="dev_user" \
  --changeset-path="main/customers/tables/customers.sql" # Use your actual path
```

**Prevention:**
- **Never modify executed changesets** in shared environments
- **Create new changesets** for additional changes
- **Use rollback and new changeset** for corrections

#### 2. **Database Connection Issues**

**Problem:** Cannot connect to database during migration.

```
Connection could not be created to jdbc:postgresql://localhost:5432/main
with driver org.postgresql.Driver.
```

**Solutions:**

**Check Database Availability:**
```bash
# Test database connection
pg_isready -h localhost -p 5432 -U postgres

# Check database exists
psql -h localhost -p 5432 -U postgres -l
```

**Verify Configuration:**
```bash
# Check liquibase.properties
cat liquibase.properties

# Test with explicit parameters
liquibase --url=jdbc:postgresql://localhost:5432/main \
          --username=postgres \
          --password=your_password \
          status
```

**Container Networking:**
```bash
# Check container connectivity
docker network ls
docker inspect [container_name]

# Test from inside container
docker exec -it [container_name] ping app-domain-db
```

#### 3. **Changelog File Not Found**

**Problem:** Liquibase cannot locate changelog files.

```
Error: Could not find changelog file 'main/changelog.xml'
```

**Solutions:**

**Verify File Paths:**
```bash
# Check current directory and search path
pwd
ls -la Liquibase/
echo $LIQUIBASE_SEARCH_PATH
```

**Container Volume Mounting:**
```bash
# Verify volume mounts
docker inspect [container_name] | grep -A 5 Mounts

# Check files inside container
docker exec -it [container_name] ls -la /liquibase/changelog/
```

**Relative Path Issues:**
```xml
<!-- Use relative paths in changelog includes (example with template entity) -->
<include file="main/tables/customers.sql"/> <!-- Replace 'cashiers' with your entity -->
<!-- NOT absolute paths -->
<include file="/liquibase/changelog/main/tables/customers.sql"/> <!-- Absolute paths are incorrect -->
```

#### 4. **PostgreSQL Permission Errors**

**Problem:** Insufficient privileges for migration operations.

```
ERROR: permission denied to create database
```

**Solutions:**

**Check User Privileges:**
```sql
-- Connect as postgres superuser
\c postgres postgres

-- Check current user permissions
\du

-- Grant necessary privileges
GRANT CREATE ON DATABASE postgres TO your_user;
ALTER USER your_user CREATEDB;
```

**Container User Issues:**
```yaml
# In docker-compose.yml, ensure proper user
app-domain-db:
  environment:
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: password@
    POSTGRES_DB: postgres
```

#### 5. **Lock Timeout Issues**

**Problem:** Migrations fail due to database locks.

```
ERROR: canceling statement due to lock timeout
```

**Solutions:**

**Investigate Locks:**
```sql
-- Check current locks
SELECT pid, state, query, query_start
FROM pg_stat_activity
WHERE state != 'idle';

-- Check lock conflicts
SELECT blocked_locks.pid AS blocked_pid,
       blocking_locks.pid AS blocking_pid,
       blocked_activity.query AS blocked_statement
FROM pg_catalog.pg_locks blocked_locks
JOIN pg_catalog.pg_locks blocking_locks
  ON blocking_locks.locktype = blocked_locks.locktype;
```

**Increase Timeout:**
```sql
--changeset dev_user:"long running migration"
SET lock_timeout = '10min';
-- Your migration statements
```

**Batch Processing:**
```sql
-- Process large updates in smaller batches (adapt table/column names to your domain)
UPDATE your_large_table SET new_column = value
WHERE id BETWEEN 1 AND 10000;
-- Commit and continue in next changeset
```

### Debugging Techniques

#### 1. **Verbose Logging**
```bash
# Enable detailed logging
liquibase --log-level=DEBUG update

# Log to file
liquibase update --log-file=migration.log
```

#### 2. **Dry Run Validation**
```bash
# Generate SQL without executing
liquibase update-sql > preview.sql

# Validate changelog
liquibase validate --verbose
```

#### 3. **Step-by-Step Execution**
```bash
# Execute one changeset at a time
liquibase update-count 1

# Check status after each step
liquibase status
```

#### 4. **Container Debugging**
```bash
# Access container shell
docker exec -it liquibase-container /bin/sh

# Check environment variables
env | grep LIQUIBASE

# Test connectivity from container
wget -qO- http://app-domain-db:5432 || echo "Connection failed"
```

### Recovery Procedures

#### 1. **Failed Migration Recovery**

**Immediate Actions:**
```bash
# Check what failed
liquibase status

# View recent changes
liquibase history --count=5

# Check database changelog
SELECT * FROM databasechangelog
WHERE exectype = 'FAILED'
ORDER BY dateexecuted DESC;
```

**Recovery Options:**

**Option A: Fix and Retry**
```bash
# Fix the problematic changeset
vim main/[domain]/[file].sql

# Clear lock if stuck
liquibase release-locks

# Retry migration
liquibase update
```

**Option B: Mark as Executed**
```bash
# Mark changeset as manually resolved
liquibase mark-next-changeset-ran
```

**Option C: Rollback and Fix**
```bash
# Rollback to last known good state
liquibase rollback-count 1

# Fix changeset and re-run
liquibase update
```

#### 2. **Disaster Recovery**

**Database Restore:**
```bash
# Restore from backup
pg_restore -h localhost -p 5432 -U postgres -d main backup.dump

# Re-run migrations from specific point
liquibase update --starting-changeset="[changeset_id]"
```

**Environment Rebuild:**
```bash
# Complete environment reset
docker compose down -v
docker compose up app-domain-db app-domain-db-migrations

# Or using Aspire
dotnet run --project src/AppDomain.AppHost
```

This comprehensive guide provides the foundation for managing database migrations with Liquibase in Momentum applications. The patterns and practices outlined here ensure reliable, scalable, and maintainable database evolution across development teams and deployment environments.
