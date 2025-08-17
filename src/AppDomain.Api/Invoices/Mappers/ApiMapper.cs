// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Queries;

namespace AppDomain.Api.Invoices.Mappers;

[Mapper]
public static partial class ApiMapper
{
    public static partial GetInvoicesQuery ToQuery(this Models.GetInvoicesRequest request, Guid tenantId);
}
