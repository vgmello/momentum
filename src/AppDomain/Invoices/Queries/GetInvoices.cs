// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Data;

namespace AppDomain.Invoices.Queries;

/// <summary>
///     Query to retrieve a paginated list of invoices within a tenant, optionally filtered by status.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="Offset">The number of records to skip for pagination (default: 0).</param>
/// <param name="Limit">The maximum number of records to return (default: 100).</param>
/// <param name="Status">Optional status filter to restrict results to invoices with specific status.</param>
public record GetInvoicesQuery(Guid TenantId, int Offset = 0, int Limit = 100, string? Status = null)
    : IQuery<IEnumerable<Invoice>>;

/// <summary>
///     Handles the GetInvoicesQuery to retrieve a filtered and paginated list of invoices from the database.
/// </summary>
public static class GetInvoicesQueryHandler
{
    /// <summary>
    ///     Retrieves invoices for the specified tenant with optional status filtering and pagination.
    /// </summary>
    /// <param name="query">The query containing tenant ID, pagination parameters, and optional status filter.</param>
    /// <param name="db">The database context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of invoices ordered by creation date (newest first).</returns>
    public static async Task<IEnumerable<Invoice>> Handle(GetInvoicesQuery query, AppDomainDb db, CancellationToken cancellationToken)
    {
        var queryable = db.Invoices
            .Where(i => i.TenantId == query.TenantId);

        if (!string.IsNullOrEmpty(query.Status))
        {
            queryable = queryable.Where(i => i.Status == query.Status);
        }

        var invoices = await queryable
            .OrderByDescending(i => i.CreatedDateUtc)
            .Skip(query.Offset)
            .Take(query.Limit)
            .Select(i => i.ToModel())
            .ToListAsync(cancellationToken);

        return invoices;
    }
}
