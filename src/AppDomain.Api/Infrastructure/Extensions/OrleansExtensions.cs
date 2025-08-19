// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Api.Infrastructure.Extensions;

public static class OrleansExtensions
{
    public static IHostApplicationBuilder AddOrleansClient(this IHostApplicationBuilder builder)
    {
        var useLocalCluster = builder.Configuration.GetValue<bool>("Orleans:UseLocalhostClustering");
        var orleansConnectionString = builder.Configuration.GetConnectionString("OrleansClustering");

        if (!useLocalCluster && string.IsNullOrEmpty(orleansConnectionString))
            throw new InvalidOperationException("Orleans 'OrleansClustering' ConnectionString is missing");

        if (!useLocalCluster)
        {
            builder.AddKeyedAzureTableServiceClient("OrleansClustering");
        }

        builder.UseOrleansClient(clientBuilder =>
        {
            if (useLocalCluster)
            {
                clientBuilder.UseLocalhostClustering();
            }
        });

        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(opt => opt.AddMeter("Microsoft.Orleans"))
            .WithTracing(tracing =>
            {
                tracing.AddSource("Microsoft.Orleans.Runtime");
                tracing.AddSource("Microsoft.Orleans.Application");
            });

        return builder;
    }
}
