// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Data;

namespace AppDomain.Invoices.Queries;

/// <summary>
/// Query to retrieve a specific invoice by ID within a tenant.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="Id">The invoice identifier.</param>
public record GetInvoiceQuery(Guid TenantId, Guid Id) : IQuery<Result<Invoice>>;

/// <summary>
/// Handles the GetInvoiceQuery to retrieve a single invoice from the database.
/// </summary>
public static class GetInvoiceQueryHandler
{
    /// <summary>
    /// Retrieves an invoice by ID for the specified tenant.
    /// </summary>
    /// <param name="query">The query containing tenant ID and invoice ID.</param>
    /// <param name="db">The database context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the invoice if found, or validation errors if not found.</returns>
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
