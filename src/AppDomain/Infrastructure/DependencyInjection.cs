// Copyright (c) ORG_NAME. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

//#if (USE_DB)
using Dapper;
using LinqToDB.AspNet;
using LinqToDB.AspNet.Logging;
using Momentum.Extensions.Data.LinqToDb;

//#endif

namespace AppDomain.Infrastructure;

/// <summary>
///     Provides extension methods for configuring AppDomain infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    ///     Registers AppDomain core services including database context and data mapping.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    public static IHostApplicationBuilder AddAppDomainServices(this IHostApplicationBuilder builder)
    {
        //#if (USE_PGSQL)
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        builder.AddNpgsqlDataSource("AppDomainDb");

        builder.Services.AddLinqToDBContext<AppDomainDb>((svcProvider, options) =>
            options
                .UseMappingSchema(schema => schema.AddMetadataReader(new SnakeCaseNamingConventionMetadataReader()))
                .UsePostgreSQL(builder.Configuration.GetConnectionString("AppDomainDb")!)
                .UseDefaultLogging(svcProvider)
        );
        //#endif

        return builder;
    }
}
