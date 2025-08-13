// Copyright (c) OrgName. All rights reserved.

using AppDomain.Core.Data;
using Dapper;
using LinqToDB;
using LinqToDB.AspNet;
using LinqToDB.AspNet.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Momentum.Extensions.Data.LinqToDb;

namespace AppDomain.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddAppDomainServices(this IHostApplicationBuilder builder)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        builder.AddNpgsqlDataSource("AppDomainDb");

        builder.Services.AddLinqToDBContext<AppDomainDb>((svcProvider, options) =>
            options
                .UseMappingSchema(schema => schema.AddMetadataReader(new SnakeCaseNamingConventionMetadataReader()))
                .UsePostgreSQL(builder.Configuration.GetConnectionString("AppDomainDb")!)
                .UseDefaultLogging(svcProvider)
        );

        return builder;
    }
}
