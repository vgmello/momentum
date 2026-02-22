// Copyright (c) OrgName. All rights reserved.

using System.Diagnostics.CodeAnalysis;

namespace AppDomain.Api;

/// <summary>
///     Provides dependency injection extension methods for API application services.
/// </summary>
[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    /// <summary>
    ///     Adds application-specific services to the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        return builder;
    }
}
