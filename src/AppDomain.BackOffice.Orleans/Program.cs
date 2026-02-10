// Copyright (c) OrgName. All rights reserved.

using AppDomain.BackOffice.Orleans;
using AppDomain.BackOffice.Orleans.Infrastructure.Extensions;
using AppDomain.Infrastructure;
//#if (USE_KAFKA)
using Momentum.Extensions.Messaging.Kafka;
//#endif
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.HealthChecks;

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
//#if (USE_KAFKA)
builder.AddKafkaMessagingExtensions();
//#endif
builder.AddOrleans();

builder.AddAppDomainServices();
builder.AddApplicationServices();

var app = builder.Build();

app.MapOrleansDashboard();
app.MapDefaultHealthCheckEndpoints();

await app.RunAsync(args);
