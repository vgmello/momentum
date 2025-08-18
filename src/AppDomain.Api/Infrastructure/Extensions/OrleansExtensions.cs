// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Api.Infrastructure.Extensions;

public static class OrleansExtensions
{
    public static IHostApplicationBuilder AddOrleansClient(this IHostApplicationBuilder builder)
    {
        builder.AddKeyedAzureTableServiceClient("OrleansClustering");

        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(opt => opt.AddMeter("Microsoft.Orleans"));

        builder.UseOrleansClient();

        return builder;
    }
}
