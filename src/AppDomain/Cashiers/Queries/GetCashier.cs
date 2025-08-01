// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Cashiers.Data;
using AppDomain.Core.Data;
using FluentValidation.Results;
using LinqToDB;

namespace AppDomain.Cashiers.Queries;

public record GetCashierQuery(Guid TenantId, Guid Id) : IQuery<Result<Cashier>>;

public static class GetCashierQueryHandler
{
    /// <summary>
    ///     Example: Get handler with the DB call in the main body, OK for simple scenarios, however, makes harder to test it via unit tests,
    ///     requiring us to the whole handler with integration tests
    /// </summary>
    public static async Task<Result<Cashier>> Handle(GetCashierQuery query, AppDomainDb db, CancellationToken cancellationToken)
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
