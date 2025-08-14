// Copyright (c) ORG_NAME. All rights reserved.

using Orleans.Configuration;

namespace AppDomain.BackOffice.Orleans.Infrastructure.Extensions;

public static class OrleansExtensions
{
    public static IHostApplicationBuilder AddOrleans(this IHostApplicationBuilder builder)
    {
        var useLocalCluster = builder.Configuration.GetValue<bool>("Orleans:UseLocalhostClustering");

        if (!useLocalCluster)
        {
            builder.AddKeyedAzureTableClient("OrleansClustering");
            builder.AddKeyedAzureTableClient("OrleansGrainState");
        }

        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(opt => opt.AddMeter("Microsoft.Orleans"));

        builder.UseOrleans(siloBuilder =>
        {
            if (useLocalCluster)
            {
                siloBuilder.UseLocalhostClustering();
            }

            siloBuilder.Configure<GrainCollectionOptions>(builder.Configuration.GetSection("Orleans:GrainCollection"));

            siloBuilder.UseDashboard(options =>
            {
                options.HostSelf = false;
                options.Host = "*";
            });
        });

        return builder;
    }

    public static WebApplication MapOrleansDashboard(this WebApplication app, string path = "/dashboard")
    {
        app.Map(path, opt => opt.UseOrleansDashboard());

        return app;
    }
}