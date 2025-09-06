// Copyright (c) OrgName. All rights reserved.

//#if (INCLUDE_SAMPLE)

using AppDomain.Cashiers.Data.Entities;
using AppDomain.Invoices.Data.Entities;
//#endif
using LinqToDB.Data;

namespace AppDomain.Core.Data;

/// <summary>
///     Primary database context for the AppDomain application using LinqToDB.
///     Provides access to domain entity tables and manages database connections.
/// </summary>
/// <param name="options">Database connection options configured for this context.</param>
public class AppDomainDb(DataOptions<AppDomainDb> options) : DataConnection(options.Options)
{
    //#if (INCLUDE_SAMPLE)
    /// <summary>
    ///     Gets the table accessor for Cashier entities.
    /// </summary>
    public ITable<Cashier> Cashiers => this.GetTable<Cashier>();

    public ITable<Invoice> Invoices => this.GetTable<Invoice>();
    //#else
    // Add your domain entities here when not using samples
    //#endif
}
