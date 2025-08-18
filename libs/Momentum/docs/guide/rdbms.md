---
title: Database Integration with RDBMS
description: Comprehensive guide to RDBMS integration in Momentum using LinqToDB, including database connections, entity mapping, query patterns, and best practices for PostgreSQL.
date: 2024-01-15
---

# Database Integration with RDBMS

Momentum provides robust RDBMS integration through LinqToDB, offering type-safe database operations with PostgreSQL. This guide covers database connectivity, entity mapping, query patterns, and best practices for building data-driven applications.

## Overview

Momentum's RDBMS integration focuses on:

- **Type-Safe Operations**: Compile-time checked queries and commands
- **High Performance**: Optimized data access with LinqToDB
- **PostgreSQL First**: Built specifically for PostgreSQL features
- **Transaction Safety**: Automatic transaction management
- **Testing Support**: Comprehensive testing with real databases

## Database Context Setup

### Core Database Context

```csharp
// Data/AppDomainDb.cs
public class AppDomainDb : DataConnection
{
    public AppDomainDb(DataOptions<AppDomainDb> options) : base(options.Options)
    {
        // Configure snake_case naming convention
        MappingSchema.SetNamingConvention(new SnakeCaseNamingConvention());
        
        // Configure enum mappings
        MappingSchema.SetDefaultFromEnumType<InvoiceStatus>(typeof(int));
        MappingSchema.SetDefaultFromEnumType<CashierStatus>(typeof(int));
        
        // Configure JSON converters for complex types
        SetupJsonConverters();
        
        // Configure audit field behavior
        SetupAuditFields();
    }

    // Table definitions
    public ITable<Cashier> Cashiers => this.GetTable<Cashier>();
    public ITable<Invoice> Invoices => this.GetTable<Invoice>();
    public ITable<OutboxEvent> OutboxEvents => this.GetTable<OutboxEvent>();
    public ITable<TenantConfiguration> TenantConfigurations => this.GetTable<TenantConfiguration>();

    private void SetupJsonConverters()
    {
        // JSON column support for complex types
        MappingSchema.SetConverter<InvoiceMetadata, string>(
            metadata => JsonSerializer.Serialize(metadata, JsonSerializerOptions.Default));
            
        MappingSchema.SetConverter<string, InvoiceMetadata>(
            json => string.IsNullOrEmpty(json) 
                ? null 
                : JsonSerializer.Deserialize<InvoiceMetadata>(json, JsonSerializerOptions.Default));

        MappingSchema.SetConverter<Dictionary<string, object>, string>(
            dict => JsonSerializer.Serialize(dict, JsonSerializerOptions.Default));
            
        MappingSchema.SetConverter<string, Dictionary<string, object>>(
            json => string.IsNullOrEmpty(json) 
                ? new Dictionary<string, object>() 
                : JsonSerializer.Deserialize<Dictionary<string, object>>(json, JsonSerializerOptions.Default) ?? new());
    }

    private void SetupAuditFields()
    {
        // Automatic handling of audit fields
        MappingSchema.SetExpressionMethod(() => DateTime.UtcNow);
    }
}
```

### Configuration in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Momentum service defaults
builder.AddServiceDefaults();

// Configure PostgreSQL database
builder.Services.AddLinqToDbContext<AppDomainDb>((provider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("AppDomain")!;
    
    options
        .UsePostgreSQL(connectionString)
        .UseDefaultLogging(provider)
        .UseMappingSchema(CreateMappingSchema())
        .UseConnectionFactory<AppDomainDb>(
            connectionString, 
            PostgreSQLTools.GetDataProvider());
});

// Configure connection pooling
builder.Services.Configure<ConnectionPoolSettings>(settings =>
{
    settings.MaxPoolSize = 100;
    settings.MinPoolSize = 10;
    settings.ConnectionLifeTime = TimeSpan.FromMinutes(30);
});

static MappingSchema CreateMappingSchema()
{
    var mappingSchema = new MappingSchema();
    
    // Data type mappings
    mappingSchema.SetDataType<Guid>(DataType.Guid);
    mappingSchema.SetDataType<DateTime>(DataType.DateTime2);
    mappingSchema.SetDataType<DateOnly>(DataType.Date);
    mappingSchema.SetDataType<TimeOnly>(DataType.Time);
    mappingSchema.SetDataType<decimal>(DataType.Decimal);
    
    // Configure snake_case naming
    mappingSchema.SetNamingConvention(new SnakeCaseNamingConvention());
    
    return mappingSchema;
}
```

## Entity Base Classes and Mapping

### DbEntity Base Class

```csharp
// Data/Entities/DbEntity.cs
[Table(Schema = "AppDomain")]
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

### Entity Examples

```csharp
// Data/Entities/Cashier.cs
[Table("cashiers")]
public record Cashier : DbEntity
{
    [PrimaryKey(order: 0)]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [PrimaryKey(order: 1)]
    [Column("cashier_id")]
    public Guid CashierId { get; set; }

    [Column("name", Length = 100, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    [Column("email", Length = 255, CanBeNull = true)]
    public string? Email { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("department", Length = 50)]
    public string Department { get; set; } = "General";

    [Column("status")]
    public CashierStatus Status { get; set; } = CashierStatus.Active;

    [Column("metadata", DataType = DataType.Json)]
    public Dictionary<string, object>? Metadata { get; set; }

    // Computed property (not stored in database)
    [NotColumn]
    public string DisplayName => $"{Name} ({Department})";
}

// Data/Entities/Invoice.cs
[Table("invoices")]
public record Invoice : DbEntity
{
    [PrimaryKey]
    [Column("invoice_id")]
    public Guid InvoiceId { get; set; }

    [Column("tenant_id", CanBeNull = false)]
    public Guid TenantId { get; set; }

    [Column("cashier_id", CanBeNull = false)]
    public Guid CashierId { get; set; }

    [Column("invoice_number", Length = 50, CanBeNull = false)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column("amount", Precision = 18, Scale = 2)]
    public decimal Amount { get; set; }

    [Column("description", Length = 1000)]
    public string Description { get; set; } = string.Empty;

    [Column("status")]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [Column("due_date", DataType = DataType.Date)]
    public DateOnly? DueDate { get; set; }

    [Column("paid_date_utc")]
    public DateTime? PaidDateUtc { get; set; }

    [Column("metadata", DataType = DataType.Json)]
    public InvoiceMetadata? Metadata { get; set; }

    // Foreign key relationship (not enforced by LinqToDB, but documented)
    [NotColumn]
    public Cashier? Cashier { get; set; }
}

// Supporting types
public record InvoiceMetadata
{
    public string? ExternalReference { get; set; }
    public Dictionary<string, string>? CustomFields { get; set; }
    public List<string>? Tags { get; set; }
    public decimal? TaxRate { get; set; }
    public string? PaymentMethod { get; set; }
}

public enum CashierStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Terminated = 4
}

public enum InvoiceStatus
{
    Draft = 0,
    Pending = 1,
    Paid = 2,
    Cancelled = 3,
    Overdue = 4,
    PartiallyPaid = 5
}
```

## Connection Management

### Connection Factory Pattern

```csharp
// Infrastructure/Database/AppDomainDbFactory.cs
public interface IAppDomainDbFactory
{
    AppDomainDb CreateConnection();
    Task<AppDomainDb> CreateConnectionAsync(CancellationToken cancellationToken = default);
}

public class AppDomainDbFactory : IAppDomainDbFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppDomainDbFactory> _logger;

    public AppDomainDbFactory(IServiceProvider serviceProvider, ILogger<AppDomainDbFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public AppDomainDb CreateConnection()
    {
        try
        {
            var options = _serviceProvider.GetRequiredService<DataOptions<AppDomainDb>>();
            return new AppDomainDb(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database connection");
            throw;
        }
    }

    public async Task<AppDomainDb> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var db = CreateConnection();
        
        // Test connection
        try
        {
            await db.QueryAsync<int>("SELECT 1", cancellationToken);
            return db;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            db.Dispose();
            throw;
        }
    }
}
```

### Connection Health Checks

```csharp
// Infrastructure/HealthChecks/DatabaseHealthCheck.cs
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IAppDomainDbFactory _dbFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IAppDomainDbFactory dbFactory, ILogger<DatabaseHealthCheck> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var db = await _dbFactory.CreateConnectionAsync(cancellationToken);
            
            // Execute a simple query to test connectivity
            var result = await db.QueryAsync<int>("SELECT 1", cancellationToken);
            
            if (result.First() == 1)
            {
                return HealthCheckResult.Healthy("Database connection is healthy");
            }
            
            return HealthCheckResult.Degraded("Database connection returned unexpected result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy($"Database connection failed: {ex.Message}");
        }
    }
}

// Registration in Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");
```

## Query Patterns and Data Access

### Basic CRUD Operations

```csharp
// Data Access Layer Examples
public static class CashierDataAccess
{
    // Create
    public static async Task<Cashier> InsertCashierAsync(
        this AppDomainDb db,
        Cashier cashier,
        CancellationToken cancellationToken = default)
    {
        cashier = cashier with 
        { 
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        };

        return await db.Cashiers.InsertWithOutputAsync(cashier, token: cancellationToken);
    }

    // Read - Single
    public static async Task<Cashier?> GetCashierAsync(
        this AppDomainDb db,
        Guid tenantId,
        Guid cashierId,
        CancellationToken cancellationToken = default)
    {
        return await db.Cashiers
            .Where(c => c.TenantId == tenantId && c.CashierId == cashierId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Read - Multiple with paging
    public static async Task<List<Cashier>> GetCashiersAsync(
        this AppDomainDb db,
        Guid tenantId,
        int offset = 0,
        int limit = 50,
        string? nameFilter = null,
        CashierStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Cashiers
            .Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrEmpty(nameFilter))
        {
            query = query.Where(c => c.Name.Contains(nameFilter));
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(c => c.Status == statusFilter.Value);
        }

        return await query
            .OrderBy(c => c.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // Update
    public static async Task<Cashier?> UpdateCashierAsync(
        this AppDomainDb db,
        Guid tenantId,
        Guid cashierId,
        string name,
        string? email,
        string department,
        int version,
        CancellationToken cancellationToken = default)
    {
        return await db.Cashiers
            .Where(c => c.TenantId == tenantId && 
                       c.CashierId == cashierId && 
                       c.Version == version)
            .UpdateWithOutputAsync(
                _ => new Cashier
                {
                    Name = name,
                    Email = email,
                    Department = department,
                    UpdatedDateUtc = DateTime.UtcNow
                },
                token: cancellationToken);
    }

    // Delete
    public static async Task<int> DeleteCashierAsync(
        this AppDomainDb db,
        Guid tenantId,
        Guid cashierId,
        CancellationToken cancellationToken = default)
    {
        return await db.Cashiers
            .Where(c => c.TenantId == tenantId && c.CashierId == cashierId)
            .DeleteAsync(token: cancellationToken);
    }

    // Bulk operations
    public static async Task<int> BulkInsertCashiersAsync(
        this AppDomainDb db,
        IEnumerable<Cashier> cashiers,
        CancellationToken cancellationToken = default)
    {
        var entitiesWithTimestamps = cashiers.Select(c => c with
        {
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        });

        return await db.BulkCopyAsync(entitiesWithTimestamps, cancellationToken);
    }
}
```

### Advanced Query Patterns

```csharp
// Complex queries with joins and aggregations
public static class AdvancedQueryPatterns
{
    // Join queries
    public static async Task<List<CashierWithStats>> GetCashiersWithStatsAsync(
        this AppDomainDb db,
        Guid tenantId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = from c in db.Cashiers
                    where c.TenantId == tenantId && c.IsActive
                    select new CashierWithStats
                    {
                        CashierId = c.CashierId,
                        Name = c.Name,
                        Email = c.Email,
                        Department = c.Department,
                        TotalInvoices = db.Invoices
                            .Where(i => i.TenantId == tenantId && 
                                       i.CashierId == c.CashierId &&
                                       (fromDate == null || i.CreatedDateUtc.Date >= fromDate.Value.ToDateTime(TimeOnly.MinValue)) &&
                                       (toDate == null || i.CreatedDateUtc.Date <= toDate.Value.ToDateTime(TimeOnly.MinValue)))
                            .Count(),
                        TotalAmount = db.Invoices
                            .Where(i => i.TenantId == tenantId && 
                                       i.CashierId == c.CashierId &&
                                       i.Status == InvoiceStatus.Paid &&
                                       (fromDate == null || i.CreatedDateUtc.Date >= fromDate.Value.ToDateTime(TimeOnly.MinValue)) &&
                                       (toDate == null || i.CreatedDateUtc.Date <= toDate.Value.ToDateTime(TimeOnly.MinValue)))
                            .Sum(i => (decimal?)i.Amount) ?? 0m,
                        LastInvoiceDate = db.Invoices
                            .Where(i => i.TenantId == tenantId && i.CashierId == c.CashierId)
                            .Max(i => (DateTime?)i.CreatedDateUtc)
                    };

        return await query.ToListAsync(cancellationToken);
    }

    // Window functions and analytics
    public static async Task<List<MonthlyInvoiceSummary>> GetMonthlyInvoiceSummaryAsync(
        this AppDomainDb db,
        Guid tenantId,
        int year,
        CancellationToken cancellationToken = default)
    {
        var query = from i in db.Invoices
                    where i.TenantId == tenantId && 
                          i.CreatedDateUtc.Year == year &&
                          i.Status != InvoiceStatus.Cancelled
                    group i by new { i.CreatedDateUtc.Month } into g
                    select new MonthlyInvoiceSummary
                    {
                        Year = year,
                        Month = g.Key.Month,
                        InvoiceCount = g.Count(),
                        TotalAmount = g.Sum(x => x.Amount),
                        AverageAmount = g.Average(x => x.Amount),
                        PaidAmount = g.Where(x => x.Status == InvoiceStatus.Paid).Sum(x => x.Amount),
                        PendingAmount = g.Where(x => x.Status == InvoiceStatus.Pending).Sum(x => x.Amount)
                    };

        return await query
            .OrderBy(s => s.Month)
            .ToListAsync(cancellationToken);
    }

    // JSON queries (PostgreSQL specific)
    public static async Task<List<Cashier>> GetCashiersWithMetadataAsync(
        this AppDomainDb db,
        Guid tenantId,
        string metadataKey,
        string metadataValue,
        CancellationToken cancellationToken = default)
    {
        return await db.Cashiers
            .Where(c => c.TenantId == tenantId && 
                       c.Metadata != null &&
                       Sql.Property<string>(c.Metadata, metadataKey) == metadataValue)
            .ToListAsync(cancellationToken);
    }

    // Full-text search
    public static async Task<List<Invoice>> SearchInvoicesAsync(
        this AppDomainDb db,
        Guid tenantId,
        string searchTerm,
        int offset = 0,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return await db.Invoices
            .Where(i => i.TenantId == tenantId &&
                       (i.InvoiceNumber.Contains(searchTerm) ||
                        i.Description.Contains(searchTerm) ||
                        Sql.Like(i.Description, $"%{searchTerm}%")))
            .OrderByDescending(i => i.CreatedDateUtc)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}

// Supporting DTOs
public record CashierWithStats
{
    public Guid CashierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Department { get; set; } = string.Empty;
    public int TotalInvoices { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? LastInvoiceDate { get; set; }
}

public record MonthlyInvoiceSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
}
```

## Transaction Handling

### Automatic Transaction Management

```csharp
// Transactions are automatically handled by the framework
public static class TransactionalOperations
{
    // This entire method runs in a single transaction
    public static async Task<Result<InvoiceCreationResult>> CreateInvoiceWithRelatedDataAsync(
        CreateInvoiceCommand command,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        // All operations below run in the same transaction
        
        // 1. Verify cashier exists
        var cashier = await db.GetCashierAsync(command.TenantId, command.CashierId, cancellationToken);
        if (cashier == null)
        {
            return Result<InvoiceCreationResult>.Failure("Cashier not found");
        }

        // 2. Create the invoice
        var invoice = new Invoice
        {
            InvoiceId = Guid.CreateVersion7(),
            TenantId = command.TenantId,
            CashierId = command.CashierId,
            InvoiceNumber = await GenerateInvoiceNumberAsync(db, command.TenantId, cancellationToken),
            Amount = command.Amount,
            Description = command.Description,
            Status = InvoiceStatus.Draft,
            DueDate = command.DueDate,
            Metadata = command.Metadata
        };

        var insertedInvoice = await db.InsertCashierAsync(invoice, cancellationToken);

        // 3. Update cashier statistics
        await UpdateCashierStatsAsync(db, command.CashierId, cancellationToken);

        // 4. Create audit log entry
        await CreateAuditLogAsync(db, command.TenantId, $"Invoice {invoice.InvoiceNumber} created", cancellationToken);

        return new InvoiceCreationResult
        {
            Invoice = insertedInvoice.ToModel(),
            InvoiceNumber = insertedInvoice.InvoiceNumber
        };

        // If any operation fails, the entire transaction is automatically rolled back
    }
}
```

### Manual Transaction Control

```csharp
// For complex scenarios requiring explicit transaction control
public static class ManualTransactionExample
{
    public static async Task<Result<BatchProcessResult>> ProcessInvoiceBatchAsync(
        BatchProcessCommand command,
        AppDomainDb db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using var transaction = await db.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var results = new List<InvoiceProcessResult>();
            var totalProcessed = 0;

            foreach (var invoiceData in command.InvoiceData)
            {
                // Process each invoice
                var invoice = new Invoice
                {
                    InvoiceId = Guid.CreateVersion7(),
                    TenantId = command.TenantId,
                    CashierId = invoiceData.CashierId,
                    InvoiceNumber = invoiceData.InvoiceNumber,
                    Amount = invoiceData.Amount,
                    Description = invoiceData.Description,
                    Status = InvoiceStatus.Draft
                };

                var insertedInvoice = await db.Invoices.InsertWithOutputAsync(invoice, token: cancellationToken);
                
                results.Add(new InvoiceProcessResult
                {
                    InvoiceId = insertedInvoice.InvoiceId,
                    InvoiceNumber = insertedInvoice.InvoiceNumber,
                    Success = true
                });

                totalProcessed++;

                // Commit in batches to avoid long-running transactions
                if (totalProcessed % 100 == 0)
                {
                    await transaction.CommitAsync(cancellationToken);
                    transaction = await db.BeginTransactionAsync(cancellationToken);
                    
                    logger.LogInformation("Processed batch of 100 invoices, total: {Total}", totalProcessed);
                }
            }

            // Final commit
            await transaction.CommitAsync(cancellationToken);

            return new BatchProcessResult
            {
                TotalProcessed = totalProcessed,
                SuccessfulResults = results,
                Success = true
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Batch processing failed, transaction rolled back");
            
            return Result<BatchProcessResult>.Failure($"Batch processing failed: {ex.Message}");
        }
    }
}
```

## Best Practices

### Performance Optimization

```csharp
// 1. Use compiled queries for frequently executed queries
public static class CompiledQueries
{
    private static readonly Func<AppDomainDb, Guid, Guid, Task<Cashier?>> GetCashierCompiled =
        CompiledQuery.Compile<AppDomainDb, Guid, Guid, Task<Cashier?>>(
            (db, tenantId, cashierId) =>
                db.Cashiers
                    .Where(c => c.TenantId == tenantId && c.CashierId == cashierId)
                    .FirstOrDefaultAsync());

    public static Task<Cashier?> GetCashierAsync(AppDomainDb db, Guid tenantId, Guid cashierId)
    {
        return GetCashierCompiled(db, tenantId, cashierId);
    }
}

// 2. Use projections to limit data transfer
public static class ProjectionExamples
{
    public static async Task<List<CashierSummary>> GetCashierSummariesAsync(
        this AppDomainDb db,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Only select needed columns
        return await db.Cashiers
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .Select(c => new CashierSummary
            {
                CashierId = c.CashierId,
                Name = c.Name,
                Department = c.Department
            })
            .ToListAsync(cancellationToken);
    }
}

// 3. Use appropriate indexes
public static class IndexingGuidance
{
    /*
    Recommended indexes for the entities above:
    
    -- Cashiers table
    CREATE INDEX IX_cashiers_tenant_id ON "AppDomain".cashiers(tenant_id);
    CREATE INDEX IX_cashiers_tenant_department ON "AppDomain".cashiers(tenant_id, department) WHERE is_active = true;
    CREATE INDEX IX_cashiers_email ON "AppDomain".cashiers(email) WHERE email IS NOT NULL;
    
    -- Invoices table
    CREATE INDEX IX_invoices_tenant_id ON "AppDomain".invoices(tenant_id);
    CREATE INDEX IX_invoices_cashier_id ON "AppDomain".invoices(tenant_id, cashier_id);
    CREATE INDEX IX_invoices_status ON "AppDomain".invoices(status) WHERE status IN (1, 4); -- Pending and Overdue
    CREATE INDEX IX_invoices_due_date ON "AppDomain".invoices(due_date) WHERE due_date IS NOT NULL;
    CREATE INDEX IX_invoices_created_date ON "AppDomain".invoices(created_date_utc DESC);
    CREATE INDEX IX_invoices_invoice_number ON "AppDomain".invoices(tenant_id, invoice_number);
    
    -- JSON indexes for metadata queries
    CREATE INDEX IX_invoices_metadata_gin ON "AppDomain".invoices USING GIN (metadata);
    CREATE INDEX IX_cashiers_metadata_gin ON "AppDomain".cashiers USING GIN (metadata);
    */
}
```

### Error Handling and Resilience

```csharp
// Connection resilience
public static class DatabaseResilience
{
    public static async Task<TResult> ExecuteWithRetryAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        int maxRetries = 3,
        TimeSpan baseDelay = default,
        CancellationToken cancellationToken = default)
    {
        if (baseDelay == default)
            baseDelay = TimeSpan.FromMilliseconds(100);

        var attempt = 0;
        
        while (true)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (IsTransientError(ex) && attempt < maxRetries)
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static bool IsTransientError(Exception ex)
    {
        return ex is Npgsql.NpgsqlException npgsqlEx &&
               (npgsqlEx.IsTransient || 
                npgsqlEx.SqlState == "40001" || // deadlock_detected
                npgsqlEx.SqlState == "40P01" || // deadlock_detected
                npgsqlEx.SqlState == "53300");  // too_many_connections
    }
}

// Usage example
public static async Task<Result<Cashier>> CreateCashierWithRetryAsync(
    CreateCashierCommand command,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    try
    {
        return await DatabaseResilience.ExecuteWithRetryAsync(async ct =>
        {
            var cashier = new Cashier
            {
                TenantId = command.TenantId,
                CashierId = Guid.CreateVersion7(),
                Name = command.Name,
                Email = command.Email,
                Department = command.Department
            };

            var inserted = await db.InsertCashierAsync(cashier, ct);
            return inserted.ToModel();
        }, cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
        return Result<Cashier>.Failure($"Failed to create cashier: {ex.Message}");
    }
}
```

### Security Considerations

```csharp
// Multi-tenancy enforcement
public static class SecurityPatterns
{
    // Always include tenant ID in queries
    public static async Task<List<Invoice>> GetInvoicesSecureAsync(
        this AppDomainDb db,
        Guid tenantId, // Always required
        Guid userId,   // For additional authorization
        CancellationToken cancellationToken = default)
    {
        // Verify user has access to this tenant
        var userTenantAccess = await db.UserTenantAccess
            .Where(uta => uta.UserId == userId && uta.TenantId == tenantId && uta.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (userTenantAccess == null)
        {
            throw new UnauthorizedAccessException("User does not have access to this tenant");
        }

        // Query always includes tenant filter
        return await db.Invoices
            .Where(i => i.TenantId == tenantId) // Critical: Always filter by tenant
            .ToListAsync(cancellationToken);
    }

    // SQL injection prevention (LinqToDB is safe by default, but for raw SQL)
    public static async Task<List<Invoice>> SearchInvoicesSecureAsync(
        this AppDomainDb db,
        Guid tenantId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        // Use parameterized queries for raw SQL
        var sql = @"
            SELECT * FROM ""AppDomain"".invoices 
            WHERE tenant_id = @tenantId 
            AND (invoice_number ILIKE @searchPattern OR description ILIKE @searchPattern)
            ORDER BY created_date_utc DESC";

        return await db.QueryAsync<Invoice>(sql, new
        {
            tenantId = tenantId,
            searchPattern = $"%{searchTerm}%"
        }, cancellationToken: cancellationToken);
    }
}
```

## Testing Database Operations

### Unit Testing with Mocks

```csharp
[Test]
public async Task GetCashierAsync_ValidIds_ReturnsCashier()
{
    // Arrange
    var tenantId = Guid.NewGuid();
    var cashierId = Guid.NewGuid();
    
    var expectedCashier = new Cashier
    {
        TenantId = tenantId,
        CashierId = cashierId,
        Name = "John Doe",
        Email = "john@example.com",
        Department = "Sales"
    };

    var mockDb = new Mock<AppDomainDb>();
    mockDb.Setup(db => db.GetCashierAsync(tenantId, cashierId, It.IsAny<CancellationToken>()))
          .ReturnsAsync(expectedCashier);

    // Act
    var result = await mockDb.Object.GetCashierAsync(tenantId, cashierId);

    // Assert
    result.Should().NotBeNull();
    result!.Name.Should().Be("John Doe");
    result.Email.Should().Be("john@example.com");
}
```

### Integration Testing with Real Database

```csharp
public class DatabaseIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public DatabaseIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InsertCashier_ValidData_InsertsSuccessfully()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();

        var cashier = new Cashier
        {
            TenantId = Guid.NewGuid(),
            CashierId = Guid.NewGuid(),
            Name = "Jane Doe",
            Email = "jane@example.com",
            Department = "Marketing",
            IsActive = true
        };

        // Act
        var inserted = await db.InsertCashierAsync(cashier);

        // Assert
        inserted.Should().NotBeNull();
        inserted.CashierId.Should().Be(cashier.CashierId);
        inserted.Name.Should().Be(cashier.Name);
        inserted.CreatedDateUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        inserted.Version.Should().BeGreaterThan(0);

        // Verify it was actually inserted
        var retrieved = await db.GetCashierAsync(cashier.TenantId, cashier.CashierId);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be(cashier.Name);
    }

    [Fact]
    public async Task UpdateCashier_OptimisticLocking_HandlesVersionConflict()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();

        var cashier = new Cashier
        {
            TenantId = Guid.NewGuid(),
            CashierId = Guid.NewGuid(),
            Name = "Original Name",
            Email = "original@example.com",
            Department = "Sales"
        };

        var inserted = await db.InsertCashierAsync(cashier);

        // Act - Try to update with wrong version
        var updated = await db.UpdateCashierAsync(
            inserted.TenantId,
            inserted.CashierId,
            "Updated Name",
            "updated@example.com",
            "Marketing",
            inserted.Version + 1); // Wrong version

        // Assert
        updated.Should().BeNull(); // Update should fail due to version mismatch
    }
}
```

## Migration and Schema Management

### Database Migrations with Liquibase

```xml
<!-- infra/AppDomain.Database/changesets/001-initial-schema.xml -->
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
                   xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                   xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog
                   http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.0.xsd">

    <changeSet id="001-create-schema" author="system">
        <sql>CREATE SCHEMA IF NOT EXISTS "AppDomain";</sql>
    </changeSet>

    <changeSet id="002-create-cashiers-table" author="system">
        <createTable schemaName="AppDomain" tableName="cashiers">
            <column name="tenant_id" type="UUID">
                <constraints nullable="false" primaryKey="true"/>
            </column>
            <column name="cashier_id" type="UUID">
                <constraints nullable="false" primaryKey="true"/>
            </column>
            <column name="name" type="VARCHAR(100)">
                <constraints nullable="false"/>
            </column>
            <column name="email" type="VARCHAR(255)"/>
            <column name="is_active" type="BOOLEAN" defaultValue="true">
                <constraints nullable="false"/>
            </column>
            <column name="department" type="VARCHAR(50)" defaultValue="General">
                <constraints nullable="false"/>
            </column>
            <column name="status" type="INTEGER" defaultValue="1">
                <constraints nullable="false"/>
            </column>
            <column name="metadata" type="JSONB"/>
            <column name="created_date_utc" type="TIMESTAMP" defaultValueComputed="(NOW() AT TIME ZONE 'UTC')">
                <constraints nullable="false"/>
            </column>
            <column name="updated_date_utc" type="TIMESTAMP" defaultValueComputed="(NOW() AT TIME ZONE 'UTC')">
                <constraints nullable="false"/>
            </column>
        </createTable>

        <createIndex indexName="IX_cashiers_tenant_id" schemaName="AppDomain" tableName="cashiers">
            <column name="tenant_id"/>
        </createIndex>
        
        <createIndex indexName="IX_cashiers_tenant_department" schemaName="AppDomain" tableName="cashiers">
            <column name="tenant_id"/>
            <column name="department"/>
        </createIndex>
    </changeSet>

    <changeSet id="003-create-invoices-table" author="system">
        <createTable schemaName="AppDomain" tableName="invoices">
            <column name="invoice_id" type="UUID">
                <constraints nullable="false" primaryKey="true"/>
            </column>
            <column name="tenant_id" type="UUID">
                <constraints nullable="false"/>
            </column>
            <column name="cashier_id" type="UUID">
                <constraints nullable="false"/>
            </column>
            <column name="invoice_number" type="VARCHAR(50)">
                <constraints nullable="false"/>
            </column>
            <column name="amount" type="DECIMAL(18,2)">
                <constraints nullable="false"/>
            </column>
            <column name="description" type="VARCHAR(1000)" defaultValue="">
                <constraints nullable="false"/>
            </column>
            <column name="status" type="INTEGER" defaultValue="0">
                <constraints nullable="false"/>
            </column>
            <column name="due_date" type="DATE"/>
            <column name="paid_date_utc" type="TIMESTAMP"/>
            <column name="metadata" type="JSONB"/>
            <column name="created_date_utc" type="TIMESTAMP" defaultValueComputed="(NOW() AT TIME ZONE 'UTC')">
                <constraints nullable="false"/>
            </column>
            <column name="updated_date_utc" type="TIMESTAMP" defaultValueComputed="(NOW() AT TIME ZONE 'UTC')">
                <constraints nullable="false"/>
            </column>
        </createTable>

        <addForeignKeyConstraint baseTableSchemaName="AppDomain" baseTableName="invoices"
                                baseColumnNames="tenant_id,cashier_id"
                                constraintName="FK_invoices_cashiers"
                                referencedTableSchemaName="AppDomain" referencedTableName="cashiers"
                                referencedColumnNames="tenant_id,cashier_id"/>

        <createIndex indexName="IX_invoices_tenant_id" schemaName="AppDomain" tableName="invoices">
            <column name="tenant_id"/>
        </createIndex>
        
        <createIndex indexName="IX_invoices_cashier_id" schemaName="AppDomain" tableName="invoices">
            <column name="tenant_id"/>
            <column name="cashier_id"/>
        </createIndex>
    </changeSet>

</databaseChangeLog>
```

## Monitoring and Observability

### Database Metrics and Logging

```csharp
// Custom logging and metrics for database operations
public static class DatabaseObservability
{
    private static readonly ActivitySource ActivitySource = new("AppDomain.Database");
    
    public static async Task<T> TrackDatabaseOperation<T>(
        string operationName,
        Func<Task<T>> operation,
        ILogger logger,
        Dictionary<string, object>? tags = null)
    {
        using var activity = ActivitySource.StartActivity(operationName);
        
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                activity?.SetTag(tag.Key, tag.Value);
            }
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.LogDebug("Starting database operation: {OperationName}", operationName);
            
            var result = await operation();
            
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("db.operation.duration_ms", stopwatch.ElapsedMilliseconds);
            
            logger.LogDebug("Database operation completed: {OperationName} in {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("db.operation.duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("db.operation.error", ex.GetType().Name);
            
            logger.LogError(ex, "Database operation failed: {OperationName} after {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}

// Usage example
public static async Task<Cashier> CreateCashierWithTrackingAsync(
    CreateCashierCommand command,
    AppDomainDb db,
    ILogger logger,
    CancellationToken cancellationToken)
{
    return await DatabaseObservability.TrackDatabaseOperation(
        "CreateCashier",
        async () =>
        {
            var cashier = new Cashier
            {
                TenantId = command.TenantId,
                CashierId = Guid.CreateVersion7(),
                Name = command.Name,
                Email = command.Email,
                Department = command.Department
            };

            return await db.InsertCashierAsync(cashier, cancellationToken);
        },
        logger,
        new Dictionary<string, object>
        {
            ["tenant_id"] = command.TenantId,
            ["operation_type"] = "insert"
        });
}
```

## Next Steps

Now that you understand RDBMS integration in Momentum, explore these related topics:

- **[DbCommand Pattern](./database/dbcommand)** - Type-safe database operations with source generation
- **[Entity Mapping](./database/entity-mapping)** - Advanced entity configuration and relationships  
- **[Transactions](./database/transactions)** - Transaction management and the outbox pattern
- **[Testing](./testing/)** - Comprehensive database testing strategies
- **[Performance](./troubleshooting#performance)** - Query optimization and monitoring
- **[Migrations](./rdbms-migrations)** - Database schema evolution and deployment strategies

## Quick Reference

### Key Classes
- `DbEntity` - Base class for all entities with audit fields and optimistic locking
- `AppDomainDb` - Main database context with table definitions
- `DatabaseHealthCheck` - Health check implementation for monitoring

### Essential Patterns
- Use composite primary keys with `tenant_id` for multi-tenancy
- Always include `tenant_id` in queries for security
- Leverage PostgreSQL JSON columns for flexible metadata
- Implement optimistic locking with `xmin` system column
- Use compiled queries for frequently executed operations

### Configuration Keys
- `ConnectionStrings:AppDomain` - Database connection string
- `Database:MaxPoolSize` - Connection pool size (default: 100)
- `Database:CommandTimeout` - Command timeout in seconds (default: 30)