// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Infrastructure;
using Momentum.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add Orleans
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering()
        .AddMemoryGrainStorage("AppDomainStorage");
});

// Add domain services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddAppDomainServices(connectionString);

var host = builder.Build();

host.Run();