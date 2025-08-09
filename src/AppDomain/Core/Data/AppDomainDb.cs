// Copyright (c) ABCDEG. All rights reserved.

<!--#if (includeSample)-->
using AppDomain.Cashiers.Data.Entities;
using AppDomain.Invoices.Data.Entities;
<!--#endif-->
using LinqToDB;
using LinqToDB.Data;

namespace AppDomain.Core.Data;

public class AppDomainDb(DataOptions<AppDomainDb> options) : DataConnection(options.Options)
{
<!--#if (includeSample)-->
    public ITable<Cashier> Cashiers => this.GetTable<Cashier>();
    public ITable<Invoice> Invoices => this.GetTable<Invoice>();
<!--#endif-->
    // Add your domain entities here when not using samples
}