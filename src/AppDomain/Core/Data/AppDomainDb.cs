// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Data.Entities;
using AppDomain.Invoices.Data.Entities;
using LinqToDB;
using LinqToDB.Data;

namespace AppDomain.Core.Data;

public class AppDomainDb(DataOptions<AppDomainDb> options) : DataConnection(options.Options)
{
    public ITable<Cashier> Cashiers => this.GetTable<Cashier>();
    public ITable<Invoice> Invoices => this.GetTable<Invoice>();
}
