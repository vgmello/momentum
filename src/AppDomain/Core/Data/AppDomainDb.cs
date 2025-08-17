// Copyright (c) ORG_NAME. All rights reserved.

//#if (INCLUDE_SAMPLE)

using AppDomain.Cashiers.Data.Entities;
using AppDomain.Invoices.Data.Entities;
//#endif
using LinqToDB.Data;

namespace AppDomain.Core.Data;

public class AppDomainDb(DataOptions<AppDomainDb> options) : DataConnection(options.Options)
{
    //#if (INCLUDE_SAMPLE)
    public ITable<Cashier> Cashiers => this.GetTable<Cashier>();

    public ITable<Invoice> Invoices => this.GetTable<Invoice>();
    //#else
    // Add your domain entities here when not using samples
    //#endif
}
