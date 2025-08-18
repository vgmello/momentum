---
title: Queries in Momentum
description: Learn how to implement queries in Momentum for efficient read operations with caching, pagination, and performance optimization.
date: 2024-01-15
---

# Queries in Momentum

Queries handle **read operations** without side effects. They follow CQRS principles with clear separation from commands and are optimized for efficient data retrieval with built-in support for pagination, projections, and caching.

> **Prerequisites**: Understanding of [Commands](./commands) and the [CQRS pattern](./). New to CQRS? Start with our [Getting Started Guide](../getting-started).

## What Are Queries?

Queries are **read operations** that retrieve data without modifying state. They're optimized for performance and follow consistent patterns:

```csharp
// Simple entity query
public record GetCashierQuery(Guid TenantId, Guid Id) : IQuery<Result<Cashier>>;

// List query with pagination
public record GetCashiersQuery(Guid TenantId, int Page = 1, int PageSize = 20) : IQuery<Result<PagedResult<Cashier>>>;

// Search query with filters
public record SearchCashiersQuery(Guid TenantId, string? SearchTerm = null, bool? IsActive = null) : IQuery<Result<List<Cashier>>>;
```

### Query Principles

| Principle     | Description                          | Benefits                                |
| ------------- | ------------------------------------ | --------------------------------------- |
| **Read-Only** | Never modify application state       | Safe to retry and cache                 |
| **Immutable** | Records that can't be changed        | Thread-safe and predictable             |
| **Optimized** | Tailored for specific read scenarios | Better performance than generic queries |
| **Simple**    | Direct database access patterns      | Easier to understand and maintain       |
| **Cacheable** | Results can be cached safely         | Improved response times                 |

### Query vs Command

| Aspect           | Queries       | Commands                     |
| ---------------- | ------------- | ---------------------------- |
| **Purpose**      | Read data     | Modify state                 |
| **Side Effects** | None          | Always                       |
| **Complexity**   | Simple        | Can be complex               |
| **Caching**      | Safe to cache | Never cache                  |
| **Retry**        | Safe to retry | Careful consideration needed |

## Basic Query Example

Here's a simple query example from the AppDomain reference implementation:

```csharp
// Queries/GetCashier.cs
using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Core.Data;
using FluentValidation.Results;
using LinqToDB;

namespace AppDomain.Cashiers.Queries;

public record GetCashierQuery(Guid TenantId, Guid Id) : IQuery<Result<Cashier>>;

public static class GetCashierQueryHandler
{
    public static async Task<Result<Cashier>> Handle(
        GetCashierQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var cashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.TenantId == query.TenantId && c.CashierId == query.Id, cancellationToken);

        if (cashier is not null)
        {
            return cashier.ToModel();
        }

        return new List<ValidationFailure> { new("Id", "Cashier not found") };
    }
}
```

## Query Handler Patterns

Unlike commands, queries typically use **direct database access** since they don't need the two-tier handler architecture:

### Single-Tier Handler Pattern

Most queries access the database directly:

```csharp
public static class GetCashierQueryHandler
{
    public static async Task<Result<Cashier>> Handle(
        GetCashierQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var cashier = await db.Cashiers
            .Where(c => c.TenantId == query.TenantId && c.CashierId == query.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return cashier?.ToModel() ??
               new List<ValidationFailure> { new("Id", "Cashier not found") };
    }
}
```

### Advanced Query Patterns

For complex scenarios, you can still separate concerns:

```csharp
public record GetCashiersQuery(Guid TenantId, int Page = 1, int PageSize = 10) : IQuery<Result<PagedResult<Cashier>>>;

public static class GetCashiersQueryHandler
{
    public static async Task<Result<PagedResult<Cashier>>> Handle(
        GetCashiersQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var cashiersQuery = db.Cashiers
            .Where(c => c.TenantId == query.TenantId)
            .OrderBy(c => c.Name);

        var totalCount = await cashiersQuery.CountAsync(cancellationToken);

        var cashiers = await cashiersQuery
            .Skip(skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var models = cashiers.Select(c => c.ToModel()).ToList();

        return new PagedResult<Cashier>
        {
            Items = models,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
```

## Query Validation

While queries typically have simpler validation than commands, you can still validate them:

```csharp
public class GetCashierValidator : AbstractValidator<GetCashierQuery>
{
    public GetCashierValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.Id).NotEmpty();
    }
}

public class GetCashiersValidator : AbstractValidator<GetCashiersQuery>
{
    public GetCashiersValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.Page).GreaterThan(0);
        RuleFor(q => q.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
```

## Common Query Patterns

### Search and Filtering

Implement flexible search with multiple filter criteria:

```csharp
public record SearchCashiersQuery(
    Guid TenantId,
    string? SearchTerm = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 10) : IQuery<Result<PagedResult<Cashier>>>;

public static class SearchCashiersQueryHandler
{
    public static async Task<Result<PagedResult<Cashier>>> Handle(
        SearchCashiersQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var cashiersQuery = db.Cashiers.Where(c => c.TenantId == query.TenantId);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            cashiersQuery = cashiersQuery.Where(c =>
                c.Name.ToLower().Contains(searchTerm) ||
                c.Email.ToLower().Contains(searchTerm));
        }

        // Apply active filter
        if (query.IsActive.HasValue)
        {
            cashiersQuery = cashiersQuery.Where(c => c.IsActive == query.IsActive.Value);
        }

        var totalCount = await cashiersQuery.CountAsync(cancellationToken);

        var skip = (query.Page - 1) * query.PageSize;
        var cashiers = await cashiersQuery
            .OrderBy(c => c.Name)
            .Skip(skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Cashier>
        {
            Items = cashiers.Select(c => c.ToModel()).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
```

### Aggregate Queries

Perform calculations and aggregations:

```csharp
public record GetCashierStatsQuery(Guid TenantId, Guid CashierId) : IQuery<Result<CashierStats>>;

public static class GetCashierStatsQueryHandler
{
    public static async Task<Result<CashierStats>> Handle(
        GetCashierStatsQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var stats = await (from c in db.Cashiers
                          join i in db.Invoices on c.CashierId equals i.CashierId into invoices
                          where c.TenantId == query.TenantId && c.CashierId == query.CashierId
                          select new CashierStats
                          {
                              CashierId = c.CashierId,
                              TotalInvoices = invoices.Count(),
                              TotalAmount = invoices.Sum(i => i.Amount),
                              PaidInvoices = invoices.Count(i => i.Status == InvoiceStatus.Paid),
                              PendingInvoices = invoices.Count(i => i.Status == InvoiceStatus.Pending)
                          })
                          .FirstOrDefaultAsync(cancellationToken);

        return stats ?? new List<ValidationFailure> { new("CashierId", "Cashier not found") };
    }
}
```

### Join Queries

Query across multiple tables:

```csharp
public record GetCashierWithInvoicesQuery(Guid TenantId, Guid Id) : IQuery<Result<CashierWithInvoices>>;

public static class GetCashierWithInvoicesQueryHandler
{
    public static async Task<Result<CashierWithInvoices>> Handle(
        GetCashierWithInvoicesQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var cashierData = await (from c in db.Cashiers
                                where c.TenantId == query.TenantId && c.CashierId == query.Id
                                select new
                                {
                                    Cashier = c,
                                    Invoices = db.Invoices
                                        .Where(i => i.CashierId == c.CashierId)
                                        .ToList()
                                })
                                .FirstOrDefaultAsync(cancellationToken);

        if (cashierData == null)
        {
            return new List<ValidationFailure> { new("Id", "Cashier not found") };
        }

        return new CashierWithInvoices
        {
            Cashier = cashierData.Cashier.ToModel(),
            Invoices = cashierData.Invoices.Select(i => i.ToModel()).ToList()
        };
    }
}
```

## Performance Optimization

### Database Indexing Strategy

Create indexes that match your query patterns:

```csharp
// Liquibase migration
public class AddCashierIndexes : Migration
{
    public override void Up()
    {
        Create.Index("IX_Cashiers_TenantId_Name")
            .OnTable("cashiers")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("name").Ascending();
    }
}
```

### Query Projections

Select only the data you need to improve performance:

```csharp
public record GetCashierSummaryQuery(Guid TenantId, Guid Id) : IQuery<Result<CashierSummary>>;

public static class GetCashierSummaryQueryHandler
{
    public static async Task<Result<CashierSummary>> Handle(
        GetCashierSummaryQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var summary = await db.Cashiers
            .Where(c => c.TenantId == query.TenantId && c.CashierId == query.Id)
            .Select(c => new CashierSummary
            {
                Id = c.CashierId,
                Name = c.Name,
                Email = c.Email,
                CreatedDate = c.CreatedDateUtc
                // Only select what you need
            })
            .FirstOrDefaultAsync(cancellationToken);

        return summary ?? new List<ValidationFailure> { new("Id", "Cashier not found") };
    }
}
```

### Async Patterns

Use async/await consistently:

```csharp
public static async Task<Result<List<Cashier>>> Handle(
    GetAllCashiersQuery query,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    var cashiers = await db.Cashiers
        .Where(c => c.TenantId == query.TenantId)
        .OrderBy(c => c.Name)
        .ToListAsync(cancellationToken);

    return cashiers.Select(c => c.ToModel()).ToList();
}
```

## Error Handling

Queries use the same `Result<T>` pattern as commands:

### Not Found Handling

```csharp
public static async Task<Result<Cashier>> Handle(
    GetCashierQuery query,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    var cashier = await db.Cashiers
        .FirstOrDefaultAsync(c => c.TenantId == query.TenantId && c.CashierId == query.Id, cancellationToken);

    if (cashier is not null)
    {
        return cashier.ToModel();
    }

    // Return validation failure for not found
    return new List<ValidationFailure> { new("Id", "Cashier not found") };
}
```

### Exception Handling

```csharp
public static async Task<Result<List<Cashier>>> Handle(
    GetCashiersQuery query,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    try
    {
        var cashiers = await db.Cashiers
            .Where(c => c.TenantId == query.TenantId)
            .ToListAsync(cancellationToken);

        return cashiers.Select(c => c.ToModel()).ToList();
    }
    catch (Exception ex)
    {
        // Log the exception
        return Result<List<Cashier>>.Failure($"Database error: {ex.Message}");
    }
}
```

## Query Caching Strategies

Implement caching for frequently accessed data:

```csharp
public static class GetCashierQueryHandler
{
    public static async Task<Result<Cashier>> Handle(
        GetCashierQuery query,
        AppDomainDb db,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"cashier_{query.TenantId}_{query.Id}";

        if (cache.TryGetValue(cacheKey, out Cashier cachedCashier))
        {
            return cachedCashier;
        }

        var cashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.TenantId == query.TenantId && c.CashierId == query.Id, cancellationToken);

        if (cashier is not null)
        {
            var result = cashier.ToModel();
            cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        return new List<ValidationFailure> { new("Id", "Cashier not found") };
    }
}
```

## Query Best Practices

### Design Guidelines

#### ✅ Do's

```csharp
// ✅ Descriptive, specific query names
public record GetCashierByIdQuery(Guid TenantId, Guid Id);
public record SearchActiveCashiersQuery(Guid TenantId, string SearchTerm);
public record GetCashierInvoiceStatsQuery(Guid TenantId, Guid CashierId, DateRange DateRange);

// ✅ Include pagination for list queries
public record GetCashiersQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20  // Reasonable default
) : IQuery<Result<PagedResult<Cashier>>>;

// ✅ Use projections for specific data needs
public record GetCashierSummaryQuery(Guid TenantId, Guid Id) : IQuery<Result<CashierSummary>>;
public record GetCashierDetailQuery(Guid TenantId, Guid Id) : IQuery<Result<CashierDetail>>;

// ✅ Provide defaults for optional parameters
public record SearchCashiersQuery(
    Guid TenantId,
    string? SearchTerm = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20
);
```

#### ❌ Don'ts

```csharp
// ❌ Generic or unclear names
public record GetDataQuery(Guid Id);                      // Too generic
public record CashierQuery(Guid TenantId);               // Not specific

// ❌ No pagination for potentially large results
public record GetAllCashiersQuery(Guid TenantId);        // Could return thousands

// ❌ Missing tenant context
public record GetCashierQuery(Guid Id);                  // No TenantId

// ❌ Complex business logic in queries
public record CalculateAndUpdateCashierStatsQuery(...);  // Should be a command
```

### Handler Implementation

#### Simple Query Handler

```csharp
// ✅ Clean, focused query handler
public static class GetCashierQueryHandler
{
    public static async Task<Result<Cashier>> Handle(
        GetCashierQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var cashier = await db.Cashiers
            .Where(c => c.TenantId == query.TenantId && c.CashierId == query.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return cashier?.ToModel() ??
               new List<ValidationFailure> { new("Id", "Cashier not found") };
    }
}
```

#### Optimized Query with Projection

```csharp
// ✅ Projection for better performance
public static class GetCashierSummaryQueryHandler
{
    public static async Task<Result<CashierSummary>> Handle(
        GetCashierSummaryQuery query,
        AppDomainDb db,
        CancellationToken cancellationToken)
    {
        var summary = await db.Cashiers
            .Where(c => c.TenantId == query.TenantId && c.CashierId == query.Id)
            .Select(c => new CashierSummary  // Project directly to DTO
            {
                Id = c.CashierId,
                Name = c.Name,
                Email = c.Email,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDateUtc
                // Only select necessary fields
            })
            .FirstOrDefaultAsync(cancellationToken);

        return summary ??
               new List<ValidationFailure> { new("Id", "Cashier not found") };
    }
}
```

### Performance Guidelines

#### Database Optimization

```sql
-- ✅ Create indexes matching query patterns

-- Single entity lookups
CREATE INDEX idx_cashiers_tenant_id ON cashiers (tenant_id, cashier_id);

-- Search queries
CREATE INDEX idx_cashiers_tenant_name ON cashiers (tenant_id, name);
CREATE INDEX idx_cashiers_tenant_email ON cashiers (tenant_id, email);

-- Full-text search
CREATE INDEX idx_cashiers_name_gin ON cashiers USING gin (name gin_trgm_ops);

-- Filtered queries
CREATE INDEX idx_cashiers_tenant_active ON cashiers (tenant_id, is_active, name);
```

#### Caching Strategy

```csharp
// ✅ Cache frequently accessed, rarely changing data
public static class GetCashierQueryHandler
{
    public static async Task<Result<Cashier>> Handle(
        GetCashierQuery query,
        AppDomainDb db,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"cashier_{query.TenantId}_{query.Id}";

        // Try cache first
        if (cache.TryGetValue(cacheKey, out Cashier? cachedCashier))
        {
            return cachedCashier!;
        }

        // Query database
        var cashier = await db.Cashiers
            .FirstOrDefaultAsync(c =>
                c.TenantId == query.TenantId &&
                c.CashierId == query.Id,
                cancellationToken);

        if (cashier != null)
        {
            var result = cashier.ToModel();

            // Cache with reasonable expiration
            cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                SlidingExpiration = TimeSpan.FromMinutes(1),
                Size = 1
            });

            return result;
        }

        return new List<ValidationFailure> { new("Id", "Cashier not found") };
    }
}
```

### Error Handling

```csharp
// ✅ Consistent error handling
public static async Task<Result<List<Cashier>>> Handle(
    GetCashiersQuery query,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    try
    {
        // Validate pagination parameters
        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
        {
            return Result<List<Cashier>>.Failure("Invalid pagination parameters");
        }

        var skip = (query.Page - 1) * query.PageSize;

        var cashiers = await db.Cashiers
            .Where(c => c.TenantId == query.TenantId)
            .OrderBy(c => c.Name)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(c => c.ToModel())
            .ToListAsync(cancellationToken);

        return cashiers;
    }
    catch (OperationCanceledException)
    {
        // Don't log cancellation as error
        throw;
    }
    catch (Exception ex)
    {
        // Log unexpected database errors
        _logger.LogError(ex, "Error querying cashiers for tenant {TenantId}", query.TenantId);
        return Result<List<Cashier>>.Failure("An error occurred while retrieving cashiers");
    }
}
```

## Testing Queries

### Unit Testing Approach

#### Test Query Logic

```csharp
[Test]
public async Task Handle_ExistingCashier_ReturnsSuccess()
{
    // Arrange
    using var testContext = CreateTestContext();
    var db = testContext.Database;

    var tenantId = Guid.NewGuid();
    var cashierId = Guid.NewGuid();

    // Seed test data
    var cashier = new Data.Entities.Cashier
    {
        TenantId = tenantId,
        CashierId = cashierId,
        Name = "John Doe",
        Email = "john@example.com",
        IsActive = true,
        CreatedDateUtc = DateTime.UtcNow,
        UpdatedDateUtc = DateTime.UtcNow
    };

    await db.Cashiers.InsertAsync(cashier);

    var query = new GetCashierQuery(tenantId, cashierId);

    // Act
    var result = await GetCashierQueryHandler.Handle(query, db, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();
    result.Value.Name.Should().Be("John Doe");
    result.Value.Email.Should().Be("john@example.com");
}

[Test]
public async Task Handle_NonExistentCashier_ReturnsNotFound()
{
    // Arrange
    using var testContext = CreateTestContext();
    var db = testContext.Database;

    var query = new GetCashierQuery(Guid.NewGuid(), Guid.NewGuid());

    // Act
    var result = await GetCashierQueryHandler.Handle(query, db, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle("Cashier not found");
}
```

#### Test Query Validation

```csharp
[TestFixture]
public class GetCashiersQueryValidatorTests
{
    private GetCashiersQueryValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new GetCashiersQueryValidator();
    }

    [Test]
    public void Validate_ValidQuery_ReturnsValid()
    {
        var query = new GetCashiersQuery(Guid.NewGuid(), 1, 20);
        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_InvalidPagination_ReturnsError()
    {
        var query = new GetCashiersQuery(Guid.NewGuid(), 0, 0);
        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(0);
    }
}
```

### Performance Testing

```csharp
[Test]
[Category("Performance")]
public async Task Handle_LargeDataSet_CompletesWithinTimeout()
{
    // Arrange
    using var testContext = CreateTestContext();
    var db = testContext.Database;

    // Seed large dataset
    await SeedLargeDataset(db, recordCount: 10000);

    var query = new SearchCashiersQuery(TestTenantId, searchTerm: "test");

    // Act & Assert
    var stopwatch = Stopwatch.StartNew();

    var result = await SearchCashiersQueryHandler.Handle(query, db, CancellationToken.None);

    stopwatch.Stop();

    result.IsSuccess.Should().BeTrue();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete in < 1s
}
```

For comprehensive integration testing examples, see our [Integration Testing Guide](../testing/integration-tests).

## Next Steps

Now that you understand queries, continue with these related topics:

### Essential Reading

1. **[Commands](./commands)** - Learn about write operations and state modification
2. **[Handlers](./handlers)** - Deep dive into handler architecture patterns
3. **[Validation](./validation)** - Query parameter validation and error handling

### Performance & Optimization

4. **[Database Integration](../database/)** - Advanced database patterns and optimization
5. **[Best Practices](../best-practices#query-optimization)** - Performance tuning and caching strategies
6. **[Service Configuration](../service-configuration/)** - Caching and observability setup

### Practical Implementation

7. **[Testing Queries](../testing/unit-tests#testing-queries)** - Unit and performance testing strategies
8. **[Error Handling](../error-handling)** - Consistent error patterns for queries
9. **[Troubleshooting](../troubleshooting#query-performance-issues)** - Common query performance issues
