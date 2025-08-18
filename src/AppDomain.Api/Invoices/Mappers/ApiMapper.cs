// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Queries;

namespace AppDomain.Api.Invoices.Mappers;

/// <summary>
///     Provides mapping functionality between REST API request models and domain queries for invoice operations.
///     This mapper transforms HTTP request models into strongly-typed query objects used by the application layer.
/// </summary>
[Mapper]
public static partial class ApiMapper
{
    /// <summary>
    ///     Converts a REST API request for retrieving invoices into a domain query.
    ///     Maps pagination parameters (limit/offset) and optional status filtering from the API request.
    /// </summary>
    /// <param name="request">The HTTP request containing pagination and filtering parameters</param>
    /// <param name="tenantId">The tenant identifier to scope the query results</param>
    /// <returns>A strongly-typed query object for retrieving invoices</returns>
    public static partial GetInvoicesQuery ToQuery(this Models.GetInvoicesRequest request, Guid tenantId);
}
