// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Api.Cashiers.Models;
using AppDomain.Cashiers.Commands;
using AppDomain.Cashiers.Queries;

namespace AppDomain.Api.Cashiers.Mappers;

/// <summary>
/// Provides mapping methods to transform REST API request models into domain commands and queries for cashier operations.
/// Uses source generation to automatically implement mapping logic for partial methods.
/// </summary>
[Mapper]
public static partial class ApiMapper
{
    /// <summary>
    /// Transforms a REST API request to create a cashier into a domain command.
    /// </summary>
    /// <param name="request">The create cashier request from the REST API.</param>
    /// <param name="tenantId">The tenant identifier for multi-tenancy support.</param>
    /// <returns>A <see cref="CreateCashierCommand"/> for domain processing.</returns>
    public static partial CreateCashierCommand ToCommand(this CreateCashierRequest request, Guid tenantId);

    /// <summary>
    /// Transforms a REST API request to update a cashier into a domain command.
    /// </summary>
    /// <param name="request">The update cashier request from the REST API.</param>
    /// <param name="tenantId">The tenant identifier for multi-tenancy support.</param>
    /// <param name="cashierId">The unique identifier of the cashier to update.</param>
    /// <returns>An <see cref="UpdateCashierCommand"/> for domain processing.</returns>
    public static partial UpdateCashierCommand ToCommand(this UpdateCashierRequest request, Guid tenantId, Guid cashierId);

    /// <summary>
    /// Transforms a REST API request to retrieve cashiers into a domain query.
    /// </summary>
    /// <param name="request">The get cashiers request from the REST API containing filtering and pagination parameters.</param>
    /// <param name="tenantId">The tenant identifier for multi-tenancy support.</param>
    /// <returns>A <see cref="GetCashiersQuery"/> for domain processing.</returns>
    public static partial GetCashiersQuery ToQuery(this GetCashiersRequest request, Guid tenantId);
}
