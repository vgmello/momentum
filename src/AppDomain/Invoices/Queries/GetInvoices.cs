// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Core.Data;
using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Data;
using LinqToDB;

namespace AppDomain.Invoices.Queries;

public record GetInvoicesQuery(Guid TenantId, int Offset = 0, int Limit = 100, string? Status = null)
    : IQuery<IEnumerable<Invoice>>;

public static class GetInvoicesQueryHandler
{
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