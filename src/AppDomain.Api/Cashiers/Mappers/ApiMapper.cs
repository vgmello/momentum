// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Api.Cashiers.Models;
using AppDomain.Cashiers.Commands;
using AppDomain.Cashiers.Queries;

namespace AppDomain.Api.Cashiers.Mappers;

[Mapper]
public static partial class ApiMapper
{
    public static partial CreateCashierCommand ToCommand(this CreateCashierRequest request, Guid tenantId);

    public static partial UpdateCashierCommand ToCommand(this UpdateCashierRequest request, Guid tenantId, Guid cashierId);

    public static partial GetCashiersQuery ToQuery(this GetCashiersRequest request, Guid tenantId);
}