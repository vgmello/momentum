---
title: Entity Mapping
description: Type-safe, high-performance database access with PostgreSQL using LinqToDB with conventions and customization options.
date: 2024-01-15
---

# Entity Mapping in Momentum

Entity mapping in Momentum uses LinqToDB to provide type-safe, high-performance database access with PostgreSQL. The mapping system follows conventions while allowing customization for complex scenarios.

## Entity Base Classes

### DbEntity Base Class

All entities inherit from `DbEntity` which provides common functionality:

```csharp
[Table(Schema = "AppDomain")]
public abstract record DbEntity
{
    [Column(SkipOnUpdate = true)]
    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedDateUtc { get; set; } = DateTime.UtcNow;

    [Column("xmin", SkipOnInsert = true, SkipOnUpdate = true)]
    [OptimisticLockProperty(VersionBehavior.Auto)]
    public int Version { get; init; } = 0;
}
```

**Key Features:**
- **Schema**: All entities belong to the "AppDomain" schema
- **Audit Fields**: Automatic created/updated timestamps
- **Optimistic Locking**: Uses PostgreSQL's `xmin` for concurrency control
- **Records**: Immutable by default, promoting safe concurrent access

## Basic Entity Mapping

### Simple Entity Example

```csharp
// Entity definition
namespace AppDomain.Cashiers.Data.Entities;

public record Cashier : DbEntity
{
    [PrimaryKey(order: 0)]
    public Guid TenantId { get; set; }

    [PrimaryKey(order: 1)]
    public Guid CashierId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    // Additional properties can be added
    public bool IsActive { get; set; } = true;
    
    [Column(CanBeNull = false)]
    public string Department { get; set; } = "General";
}
```

### Database Table

This maps to a PostgreSQL table:

```sql
CREATE TABLE "AppDomain".cashiers (
    tenant_id UUID NOT NULL,
    cashier_id UUID NOT NULL,
    name VARCHAR NOT NULL,
    email VARCHAR NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    department VARCHAR NOT NULL DEFAULT 'General',
    created_date_utc TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    updated_date_utc TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    
    PRIMARY KEY (tenant_id, cashier_id)
);
```

## Advanced Entity Mapping

### Complex Entity with Relationships

```csharp
namespace AppDomain.Invoices.Data.Entities;

[Table("invoices")]
public record Invoice : DbEntity
{
    [PrimaryKey]
    public Guid InvoiceId { get; set; }

    [Column(CanBeNull = false)]
    public Guid TenantId { get; set; }

    [Column(CanBeNull = false)]
    public Guid CashierId { get; set; }

    [Column("amount", Precision = 18, Scale = 2)]
    public decimal Amount { get; set; }

    [Column("description", Length = 500)]
    public string Description { get; set; } = string.Empty;

    [Column("status")]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [Column("due_date")]
    public DateOnly? DueDate { get; set; }

    [Column("paid_date_utc")]
    public DateTime? PaidDateUtc { get; set; }

    // JSON column for metadata
    [Column("metadata", DataType = DataType.Json)]
    public InvoiceMetadata? Metadata { get; set; }

    // Computed column example
    [Column("days_overdue", IsColumn = false)]
    public int DaysOverdue => DueDate.HasValue && DateTime.UtcNow.Date > DueDate.Value.ToDateTime(TimeOnly.MinValue)
        ? (DateTime.UtcNow.Date - DueDate.Value.ToDateTime(TimeOnly.MinValue)).Days
        : 0;
}

// Supporting types
public record InvoiceMetadata
{
    public string? ExternalReference { get; set; }
    public Dictionary<string, string>? CustomFields { get; set; }
    public List<string>? Tags { get; set; }
}

public enum InvoiceStatus
{
    Draft = 0,
    Pending = 1,
    Paid = 2,
    Cancelled = 3,
    Overdue = 4
}
```

### Corresponding SQL Table

```sql
CREATE TABLE "AppDomain".invoices (
    invoice_id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    cashier_id UUID NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    description VARCHAR(500) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    due_date DATE NULL,
    paid_date_utc TIMESTAMP NULL,
    metadata JSONB NULL,
    created_date_utc TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    updated_date_utc TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    
    FOREIGN KEY (tenant_id, cashier_id) REFERENCES "AppDomain".cashiers(tenant_id, cashier_id)
);
```

## Column Mapping Attributes

### Common Column Attributes

```csharp
public record Product : DbEntity
{
    [PrimaryKey]
    public Guid ProductId { get; set; }

    // Custom column name
    [Column("product_name")]
    public string Name { get; set; } = string.Empty;

    // Length constraint
    [Column("sku", Length = 50)]
    public string SKU { get; set; } = string.Empty;

    // Precision and scale for decimals
    [Column("price", Precision = 10, Scale = 2)]
    public decimal Price { get; set; }

    // Nullable column
    [Column("discount_rate", CanBeNull = true)]
    public decimal? DiscountRate { get; set; }

    // Skip on operations
    [Column("calculated_field", SkipOnInsert = true, SkipOnUpdate = true)]
    public string CalculatedField { get; set; } = string.Empty;

    // Data type specification
    [Column("tags", DataType = DataType.Json)]
    public List<string>? Tags { get; set; }

    // Not mapped to database
    [NotColumn]
    public string DisplayName => $"{Name} ({SKU})";
}
```

### Custom Data Types

```csharp
public record Customer : DbEntity
{
    [PrimaryKey]
    public Guid CustomerId { get; set; }

    // Custom converter for complex types
    [Column("address", DataType = DataType.Json)]
    public Address Address { get; set; } = new();

    // Enum mapping
    [Column("customer_type")]
    public CustomerType Type { get; set; }

    // Date-only mapping
    [Column("birth_date", DataType = DataType.Date)]
    public DateOnly? BirthDate { get; set; }

    // Time-only mapping
    [Column("preferred_contact_time", DataType = DataType.Time)]
    public TimeOnly? PreferredContactTime { get; set; }
}

public record Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public enum CustomerType
{
    Individual = 1,
    Business = 2,
    Enterprise = 3
}
```

## Composite Keys and Indexes

### Multi-Column Primary Keys

```csharp
public record CashierCurrency : DbEntity
{
    [PrimaryKey(order: 0)]
    public Guid TenantId { get; set; }

    [PrimaryKey(order: 1)]
    public Guid CashierId { get; set; }

    [PrimaryKey(order: 2)]
    public string CurrencyCode { get; set; } = string.Empty;

    public decimal ExchangeRate { get; set; } = 1.0m;
    public bool IsDefault { get; set; } = false;
}
```

### Index Definitions

```csharp
// Using fluent mapping for complex scenarios
public class InvoiceMapping : EntityMappingBuilder<Invoice>
{
    public override void Configure()
    {
        // Composite index
        HasIndex(nameof(Invoice.TenantId), nameof(Invoice.Status), nameof(Invoice.DueDate))
            .HasName("IX_invoices_tenant_status_due_date");

        // Unique index
        HasIndex(nameof(Invoice.TenantId), "external_reference")
            .IsUnique()
            .HasName("IX_invoices_external_reference");

        // Partial index (PostgreSQL specific)
        HasIndex(nameof(Invoice.Status))
            .HasFilter("status IN (1, 4)") // Only for Pending and Overdue
            .HasName("IX_invoices_active_status");
    }
}
```

## Navigation and Relationships

### Foreign Key Relationships

While LinqToDB doesn't have built-in navigation properties like Entity Framework, you can define relationships:

```csharp
public record Invoice : DbEntity
{
    // ... other properties

    [Column("cashier_id")]
    public Guid CashierId { get; set; }

    // Navigation property (not mapped)
    [NotColumn]
    public Cashier? Cashier { get; set; }
}

// Extension method for loading relationships
public static class InvoiceExtensions
{
    public static async Task<Invoice> LoadCashierAsync(this Invoice invoice, AppDomainDb db, CancellationToken cancellationToken = default)
    {
        var cashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.CashierId == invoice.CashierId, cancellationToken);

        return invoice with { Cashier = cashier };
    }
}

// Usage
var invoice = await db.Invoices
    .FirstAsync(i => i.InvoiceId == invoiceId);

invoice = await invoice.LoadCashierAsync(db);
```

### Join Queries

```csharp
// Method in your repository or query handler
public static async Task<List<InvoiceWithCashier>> GetInvoicesWithCashiers(
    AppDomainDb db, 
    Guid tenantId, 
    CancellationToken cancellationToken)
{
    var query = from i in db.Invoices
                join c in db.Cashiers on new { i.TenantId, i.CashierId } equals new { c.TenantId, c.CashierId }
                where i.TenantId == tenantId
                select new InvoiceWithCashier
                {
                    InvoiceId = i.InvoiceId,
                    Amount = i.Amount,
                    Description = i.Description,
                    Status = i.Status,
                    CashierName = c.Name,
                    CashierEmail = c.Email
                };

    return await query.ToListAsync(cancellationToken);
}

public record InvoiceWithCashier
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public string? CashierEmail { get; set; }
}
```

## Database Context Configuration

### DbContext Setup

```csharp
public class AppDomainDb : DataConnection
{
    public AppDomainDb(DataOptions<AppDomainDb> options) : base(options.Options)
    {
        // Configure mapping options
        MappingSchema.SetDefaultFromEnumType<InvoiceStatus>(typeof(int));
        MappingSchema.SetDefaultFromEnumType<CustomerType>(typeof(int));
        
        // Custom converters
        MappingSchema.SetConverter<Address, string>(address => JsonSerializer.Serialize(address));
        MappingSchema.SetConverter<string, Address>(json => JsonSerializer.Deserialize<Address>(json) ?? new Address());
    }

    public ITable<Cashier> Cashiers => this.GetTable<Cashier>();
    public ITable<Invoice> Invoices => this.GetTable<Invoice>();
    public ITable<CashierCurrency> CashierCurrencies => this.GetTable<CashierCurrency>();
    public ITable<Customer> Customers => this.GetTable<Customer>();
}
```

### Configuration in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure database
builder.Services.AddLinqToDbContext<AppDomainDb>((provider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("AppDomain")!;
    
    options
        .UsePostgreSQL(connectionString)
        .UseDefaultLogging(provider) // Integrate with ILogger
        .UseMappingSchema(CreateMappingSchema()); // Custom mapping schema
});

static MappingSchema CreateMappingSchema()
{
    var mappingSchema = new MappingSchema();
    
    // Global configurations
    mappingSchema.SetDataType<Guid>(DataType.Guid);
    mappingSchema.SetDataType<DateTime>(DataType.DateTime2);
    mappingSchema.SetDataType<DateOnly>(DataType.Date);
    mappingSchema.SetDataType<TimeOnly>(DataType.Time);
    
    // Enum mappings
    mappingSchema.SetDefaultFromEnumType<InvoiceStatus>(typeof(int));
    mappingSchema.SetDefaultFromEnumType<CustomerType>(typeof(int));
    
    // JSON converters
    SetupJsonConverters(mappingSchema);
    
    return mappingSchema;
}

static void SetupJsonConverters(MappingSchema mappingSchema)
{
    // Generic JSON converter for complex types
    mappingSchema.SetConverter<InvoiceMetadata, string>(
        metadata => JsonSerializer.Serialize(metadata, JsonSerializerOptions.Default));
        
    mappingSchema.SetConverter<string, InvoiceMetadata>(
        json => string.IsNullOrEmpty(json) 
            ? null 
            : JsonSerializer.Deserialize<InvoiceMetadata>(json, JsonSerializerOptions.Default));
}
```

## Model Mapping Extensions

### Entity to Model Conversion

```csharp
// Extension methods for entity-to-model mapping
public static class CashierMappingExtensions
{
    public static Contracts.Models.Cashier ToModel(this Data.Entities.Cashier entity)
    {
        return new Contracts.Models.Cashier
        {
            Id = entity.CashierId,
            TenantId = entity.TenantId,
            Name = entity.Name,
            Email = entity.Email,
            IsActive = entity.IsActive,
            Department = entity.Department,
            CreatedDate = entity.CreatedDateUtc,
            UpdatedDate = entity.UpdatedDateUtc,
            Version = entity.Version
        };
    }

    public static Data.Entities.Cashier ToEntity(this Contracts.Models.Cashier model)
    {
        return new Data.Entities.Cashier
        {
            CashierId = model.Id,
            TenantId = model.TenantId,
            Name = model.Name,
            Email = model.Email,
            IsActive = model.IsActive,
            Department = model.Department,
            CreatedDateUtc = model.CreatedDate,
            UpdatedDateUtc = model.UpdatedDate,
            Version = model.Version
        };
    }
}

public static class InvoiceMappingExtensions
{
    public static Contracts.Models.Invoice ToModel(this Data.Entities.Invoice entity)
    {
        return new Contracts.Models.Invoice
        {
            Id = entity.InvoiceId,
            TenantId = entity.TenantId,
            CashierId = entity.CashierId,
            Amount = entity.Amount,
            Description = entity.Description,
            Status = (Contracts.Models.InvoiceStatus)(int)entity.Status,
            DueDate = entity.DueDate,
            PaidDate = entity.PaidDateUtc,
            Metadata = entity.Metadata?.ToModel(),
            CreatedDate = entity.CreatedDateUtc,
            UpdatedDate = entity.UpdatedDateUtc,
            Version = entity.Version
        };
    }
}
```

## Snake Case Convention

### Automatic Snake Case Naming

```csharp
public class SnakeCaseNamingConvention : IColumnAliasProvider
{
    public string GetColumnAlias(string columnName)
    {
        return ToSnakeCase(columnName);
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder();
        
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            
            if (char.IsUpper(c))
            {
                if (i > 0)
                    result.Append('_');
                
                result.Append(char.ToLower(c));
            }
            else
            {
                result.Append(c);
            }
        }
        
        return result.ToString();
    }
}

// Apply globally
builder.Services.Configure<LinqToDbSettings>(settings =>
{
    settings.DefaultSettings = new DataOptions()
        .UseNamingConvention<SnakeCaseNamingConvention>();
});
```

## Best Practices

### Entity Design

1. **Use Records**: Prefer records for immutability and value semantics
2. **Inherit from DbEntity**: Use the base class for consistent audit fields
3. **Explicit Mapping**: Be explicit about column mappings for clarity
4. **Validate Data Types**: Choose appropriate data types for your use cases

### Performance

1. **Index Strategy**: Add indexes for frequently queried columns
2. **Avoid N+1**: Use joins instead of multiple queries
3. **Projection**: Use Select() to limit returned columns
4. **Compiled Queries**: Use compiled queries for frequently executed queries

### Naming Conventions

1. **Consistent Naming**: Use consistent naming for tables and columns
2. **Snake Case**: Follow PostgreSQL conventions with snake_case
3. **Meaningful Names**: Use descriptive names for entities and properties
4. **Prefix/Suffix**: Use consistent prefixes or suffixes for related entities

### Schema Management

1. **Schema Organization**: Group related entities in schemas
2. **Version Control**: Keep entity definitions in version control
3. **Migration Strategy**: Plan for schema changes and migrations
4. **Documentation**: Document entity relationships and business rules

## Testing Entity Mapping

### Unit Tests for Mapping

```csharp
[Test]
public void ToModel_ValidEntity_MapsAllProperties()
{
    // Arrange
    var entity = new Data.Entities.Cashier
    {
        TenantId = Guid.NewGuid(),
        CashierId = Guid.NewGuid(),
        Name = "John Doe",
        Email = "john@example.com",
        IsActive = true,
        Department = "Sales",
        CreatedDateUtc = DateTime.UtcNow.AddDays(-1),
        UpdatedDateUtc = DateTime.UtcNow,
        Version = 1
    };

    // Act
    var model = entity.ToModel();

    // Assert
    model.Id.Should().Be(entity.CashierId);
    model.TenantId.Should().Be(entity.TenantId);
    model.Name.Should().Be(entity.Name);
    model.Email.Should().Be(entity.Email);
    model.IsActive.Should().Be(entity.IsActive);
    model.Department.Should().Be(entity.Department);
    model.CreatedDate.Should().Be(entity.CreatedDateUtc);
    model.UpdatedDate.Should().Be(entity.UpdatedDateUtc);
    model.Version.Should().Be(entity.Version);
}
```

### Integration Tests with Database

```csharp
[Test]
public async Task Database_InsertAndRetrieve_MaintainsDataIntegrity()
{
    // Arrange
    using var testContext = new IntegrationTestContext();
    var db = testContext.CreateDatabase<AppDomainDb>();

    var entity = new Data.Entities.Cashier
    {
        TenantId = Guid.NewGuid(),
        CashierId = Guid.NewGuid(),
        Name = "Jane Doe",
        Email = "jane@example.com",
        IsActive = true,
        Department = "Marketing"
    };

    // Act
    var insertedId = await db.InsertWithIdentityAsync(entity);
    var retrieved = await db.Cashiers
        .FirstOrDefaultAsync(c => c.CashierId == entity.CashierId);

    // Assert
    retrieved.Should().NotBeNull();
    retrieved!.Name.Should().Be(entity.Name);
    retrieved.Email.Should().Be(entity.Email);
    retrieved.IsActive.Should().Be(entity.IsActive);
    retrieved.Department.Should().Be(entity.Department);
    retrieved.Version.Should().BeGreaterThan(0); // xmin should be set
}
```

## Next Steps

- Learn about [DbCommand](./dbcommand) patterns for database operations
- Understand [Transactions](./transactions) for complex operations
- Explore [Best Practices](../best-practices) for performance optimization
- See [RDBMS Migrations](../rdbms-migrations) for schema evolution