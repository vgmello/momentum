// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Infrastructure;
using Momentum.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

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
