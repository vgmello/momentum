// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Infrastructure;
using Momentum.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add Orleans
builder.UseOrleans(siloBuilder =>
{
    var useLocalhostClustering = builder.Configuration.GetValue<bool>("Orleans:UseLocalhostClustering", true);
    
    if (useLocalhostClustering)
    {
        siloBuilder.UseLocalhostClustering();
    }
    else
    {
        // Use Azure Storage clustering when configured via Aspire
        siloBuilder.UseAzureStorageClustering(options =>
        {
            options.ConfigureTableServiceClient(builder.Configuration.GetConnectionString("OrleansClustering"));
        });
    }
    
    siloBuilder.AddMemoryGrainStorage("AppDomainStorage");
    
    // Configure persistent grain storage if available
    var grainStorageConnectionString = builder.Configuration.GetConnectionString("OrleansGrainState");
    if (!string.IsNullOrEmpty(grainStorageConnectionString))
    {
        siloBuilder.AddAzureTableGrainStorage("Default", options =>
        {
            options.ConfigureTableServiceClient(grainStorageConnectionString);
        });
    }
});

//#if (USE_PGSQL)
// Add domain services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddAppDomainServices(connectionString);
//#endif

//#if (USE_KAFKA)
builder.Services.AddKafkaMessaging(builder.Configuration);
//#endif

var host = builder.Build();

host.Run();
