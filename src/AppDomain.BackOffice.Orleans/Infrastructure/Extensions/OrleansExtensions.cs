// Copyright (c) ORG_NAME. All rights reserved.

using Orleans.Configuration;

namespace AppDomain.BackOffice.Orleans.Infrastructure.Extensions;

/// <summary>
///     Provides extension methods for configuring Microsoft Orleans in the application.
/// </summary>
public static class OrleansExtensions
{
    /// <summary>
    ///     Adds and configures Orleans silo with clustering, grain state persistence, and monitoring.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The host application builder for method chaining.</returns>
    public static IHostApplicationBuilder AddOrleans(this IHostApplicationBuilder builder)
    {
        var useLocalCluster = builder.Configuration.GetValue<bool>("Orleans:UseLocalhostClustering");

        if (!useLocalCluster)
        {
            var connectionStringName = builder.Configuration.GetValue<string>("Orleans:Clustering:ServiceKey") ?? "Orleans";
            var orleansConnectionString = builder.Configuration.GetConnectionString(connectionStringName);

            if (string.IsNullOrEmpty(orleansConnectionString))
                throw new InvalidOperationException($"Orleans '{orleansConnectionString}' connection string is not configured.");

            builder.AddKeyedAzureTableServiceClient(connectionStringName);
            builder.AddKeyedAzureBlobServiceClient("OrleansGrainState");
        }

        builder.UseOrleans(siloBuilder =>
        {
            if (useLocalCluster)
            {
                siloBuilder.UseLocalhostClustering();
            }

            siloBuilder.Configure<ClusterOptions>(opt =>
            {
                opt.ClusterId = "app-domain--cluster";
                opt.ServiceId = "AppDomain-BackOffice-Orleans";
            });

            siloBuilder.Configure<GrainCollectionOptions>(builder.Configuration.GetSection("Orleans:GrainCollection"));

            siloBuilder.UseDashboard(opt =>
            {
                opt.HostSelf = false;
                opt.Host = "*";
            });
        });

        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(opt => opt.AddMeter("Microsoft.Orleans"));

        return builder;
    }

    /// <summary>
    ///     Maps the Orleans dashboard to the specified path for monitoring and debugging Orleans grains.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="path">The path to map the dashboard to. Defaults to "/dashboard".</param>
    /// <returns>The web application for method chaining.</returns>
    public static WebApplication MapOrleansDashboard(this WebApplication app, string path = "/dashboard")
    {
        app.Map(path, opt => opt.UseOrleansDashboard());

        return app;
    }
}
