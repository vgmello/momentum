// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Data;

namespace AppDomain.Invoices.Queries;

public record GetInvoiceQuery(Guid TenantId, Guid Id) : IQuery<Result<Invoice>>;

public static class GetInvoiceQueryHandler
{
    public static async Task<Result<Invoice>> Handle(GetInvoiceQuery query, AppDomainDb db, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.TenantId == query.TenantId && i.InvoiceId == query.Id, cancellationToken);

        if (invoice is null)
        {
            return new List<ValidationFailure> { new("Id", "Invoice not found") };
        }

        return invoice.ToModel();
    }
}
