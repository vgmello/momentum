// Copyright (c) OrgName. All rights reserved.

using AppDomain.Infrastructure;
using AppDomain.BackOffice.Orleans;
using AppDomain.BackOffice.Orleans.Infrastructure.Extensions;
using Momentum.Extensions.Messaging.Kafka;
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
