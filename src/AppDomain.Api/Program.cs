// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Api.Infrastructure.Extensions;
using AppDomain.Infrastructure;
using Momentum.Extensions.Messaging.Kafka;
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;
using Momentum.ServiceDefaults.HealthChecks;

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
builder.AddApiServiceDefaults();
//#if (USE_KAFKA)
builder.AddKafkaMessagingExtensions();
//#endif
//#if (INCLUDE_ORLEANS)
builder.AddOrleansClient();
//#endif

builder.AddAppDomainServices();
builder.AddApplicationServices();

var app = builder.Build();

app.ConfigureApiUsingDefaults(requireAuth: false);
app.MapDefaultHealthCheckEndpoints();

await app.RunAsync(args);
