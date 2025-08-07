# Queries in Momentum

Queries handle read operations in your application without causing side effects. They follow CQRS principles by being separated from commands and optimized for data retrieval.

## Query Definition

Queries are immutable records that implement `IQuery<TResult>`:

```csharp
public record GetCashierQuery(Guid TenantId, Guid Id) : IQuery<Result<Cashier>>;
```

### Query Characteristics

- **Read-only**: Queries never modify state
- **Immutable**: Queries are records that cannot be changed after creation
- **Fast**: Optimized for data retrieval
- **Simple**: Generally simpler than commands

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

### Direct Database Access Pattern

For simple queries, you can access the database directly in the handler:

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

### Complex Query Pattern

For more complex queries, you might want to separate the database logic:

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

## Advanced Query Patterns

### Search Queries

Implement text search and filtering:

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

## Query Optimization

### Database Indexes

Ensure your queries have appropriate database indexes:

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

### Projection Queries

Use projections to minimize data transfer:

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

## Caching Queries

For frequently accessed data, consider caching:

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

## Best Practices

### Query Design

1. **Use descriptive names**: `GetCashierQuery`, `SearchCashiersQuery`
2. **Keep queries simple**: One query should have one purpose
3. **Make queries immutable**: Always use records
4. **Include pagination**: For queries that might return many results

### Handler Design

1. **Direct database access**: For simple queries, access the database directly
2. **Use projections**: Only select the data you need
3. **Handle not found**: Return meaningful errors for missing data
4. **Use cancellation tokens**: Support query cancellation

### Performance

1. **Add database indexes**: Ensure your queries are properly indexed
2. **Use async/await**: Don't block threads
3. **Limit result sets**: Always paginate large result sets
4. **Consider caching**: Cache frequently accessed, rarely changing data

### Error Handling

1. **Use Result pattern**: Consistent error handling across the application
2. **Validate inputs**: Validate query parameters
3. **Log exceptions**: Always log database exceptions
4. **Provide context**: Error messages should help users understand the problem

## Testing Queries

See our [Testing Guide](../testing/) for comprehensive examples of testing queries, including:

- Unit testing query handlers
- Testing with in-memory databases
- Integration testing with real databases
- Mocking database contexts

## Next Steps

- Learn about [Handlers](./handlers) architecture
- Understand [Validation](./validation) in detail
- Explore [Database Integration](../database/) patterns
- See [Commands](./commands) for write operations