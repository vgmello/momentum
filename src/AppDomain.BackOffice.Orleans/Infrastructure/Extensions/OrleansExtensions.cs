// Copyright (c) OrgName. All rights reserved.

using Orleans.Configuration;
using Orleans.Dashboard;

namespace AppDomain.BackOffice.Orleans.Infrastructure.Extensions;

/// <summary>
///     Provides extension methods for configuring Microsoft Orleans in the application.
/// </summary>
[ExcludeFromCodeCoverage]
public static class OrleansExtensions
{
    public const string SectionName = "Orleans";
    public const string GrainDirectoryName = "Default";

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
        string? grainDirectoryConnectionString = null;

        if (!useLocalCluster)
        {
            var clusterServiceName = GetServiceName(config, "Clustering:ServiceKey", SectionName);
            var grainStateServiceName = GetServiceName(config, "GrainStorage:Default:ServiceKey", SectionName);

            var orleansConnectionString = builder.Configuration.GetConnectionString(clusterServiceName);

            if (string.IsNullOrEmpty(orleansConnectionString))
                throw new InvalidOperationException($"Orleans connection string '{orleansConnectionString}' is not set.");

            var grainDirectoryServiceName = GetServiceName(config, "GrainDirectory:Default:ServiceKey", "GrainDirectory");

            // Fall back to clustering connection string when grain directory has no dedicated connection
            // (e.g., Docker Compose where only ConnectionStrings:Orleans is set)
            if (string.IsNullOrEmpty(builder.Configuration.GetConnectionString(grainDirectoryServiceName)))
            {
                grainDirectoryServiceName = clusterServiceName;
                config["GrainDirectory:Default:ServiceKey"] = clusterServiceName;
            }

            grainDirectoryConnectionString = builder.Configuration.GetConnectionString(grainDirectoryServiceName)
                                            ?? orleansConnectionString;

            builder.AddKeyedAzureTableServiceClient(clusterServiceName);
            builder.AddKeyedAzureBlobServiceClient(grainStateServiceName);

            if (grainDirectoryServiceName != clusterServiceName)
                builder.AddKeyedAzureTableServiceClient(grainDirectoryServiceName);
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

            if (!useLocalCluster)
            {
                siloBuilder.AddAzureTableGrainDirectory(
                    GrainDirectoryName,
                    options => options.TableServiceClient = new Azure.Data.Tables.TableServiceClient(grainDirectoryConnectionString));
            }

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
