// Copyright (c) OrgName. All rights reserved.

using Orleans.Configuration;
using Orleans.Dashboard;

namespace AppDomain.BackOffice.Orleans.Infrastructure.Extensions;

/// <summary>
///     Provides extension methods for configuring Microsoft Orleans in the application.
/// </summary>
public static class OrleansExtensions
{
    public const string SectionName = "Orleans";

    /// <summary>
    ///     Adds and configures Orleans silo with clustering, grain state persistence, and monitoring.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionName">Section Name</param>
    /// <returns>The host application builder for method chaining.</returns>
    public static IHostApplicationBuilder AddOrleans(this IHostApplicationBuilder builder, string sectionName = SectionName)
    {
        var config = builder.Configuration.GetSection(sectionName);

        var useLocalCluster = config.GetValue<bool>("UseLocalhostClustering");

        if (!useLocalCluster)
        {
            var clusterServiceName = GetServiceName(config, "Clustering:ServiceKey", SectionName);
            var grainStateServiceName = GetServiceName(config, "GrainStorage:Default:ServiceKey", SectionName);

            var orleansConnectionString = builder.Configuration.GetConnectionString(clusterServiceName);

            if (string.IsNullOrEmpty(orleansConnectionString))
                throw new InvalidOperationException($"Orleans connection string '{orleansConnectionString}' is not set.");

            builder.AddKeyedAzureTableServiceClient(clusterServiceName);
            builder.AddKeyedAzureBlobServiceClient(grainStateServiceName);
        }
        else
        {
            SetupLocalCluster(config);
        }

        builder.UseOrleans(siloBuilder =>
        {
            if (useLocalCluster)
            {
                siloBuilder.UseLocalhostClustering();
            }

            siloBuilder.Configure<GrainCollectionOptions>(builder.Configuration.GetSection("Orleans:GrainCollection"));

            siloBuilder.AddDashboard();
        });

        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(opt => opt.AddMeter("Microsoft.Orleans"));

        return builder;
    }

    private static string GetServiceName(IConfigurationSection config, string keyPath, string defaultValue)
    {
        var serviceName = config.GetValue<string>(keyPath);

        if (serviceName is null)
        {
            serviceName = defaultValue;
            config[keyPath] = serviceName;
        }

        return serviceName;
    }

    private static void SetupLocalCluster(IConfigurationSection config)
    {
        config["Clustering:ProviderType"] = "Development";
        config["GrainStorage:Default:ProviderType"] = "Memory";
    }

    /// <summary>
    ///     Maps the Orleans dashboard to the specified path for monitoring and debugging Orleans grains.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="path">The path to map the dashboard to. Defaults to "/dashboard".</param>
    /// <returns>The web application for method chaining.</returns>
    public static WebApplication MapDashboard(this WebApplication app, string path = "/dashboard")
    {
        app.MapOrleansDashboard(routePrefix: path);

        return app;
    }
}
