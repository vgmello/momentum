// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.BackOffice.Orleans;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDataSource("AppDomainDb");

        return builder;
    }
}
