// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.BackOffice;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDataSource("AppDomainDb");

        return builder;
    }
}
