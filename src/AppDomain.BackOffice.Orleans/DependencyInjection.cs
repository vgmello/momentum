// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.BackOffice.Orleans;

/// <summary>
///     Provides dependency injection configuration for the Orleans stateful processing service.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    ///     Adds application-specific services required for Orleans grain operations.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The configured host application builder.</returns>
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        return builder;
    }
}
