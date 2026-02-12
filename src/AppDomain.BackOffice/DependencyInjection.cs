// Copyright (c) OrgName. All rights reserved.

//#if (INCLUDE_SAMPLE)
using AppDomain.BackOffice.Invoices.Jobs;
//#endif

namespace AppDomain.BackOffice;

/// <summary>
///     Provides dependency injection configuration for the back office service.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    ///     Adds application-specific services for background processing operations.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        //#if (INCLUDE_SAMPLE)
        builder.Services.AddHostedService<DummyInvoiceGenerator>();
        //#endif

        return builder;
    }
}
