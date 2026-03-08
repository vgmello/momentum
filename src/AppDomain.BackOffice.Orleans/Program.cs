// Copyright (c) OrgName. All rights reserved.

using AppDomain.BackOffice.Orleans;
using AppDomain.BackOffice.Orleans.Infrastructure.Extensions;
using AppDomain.Infrastructure;
//#if (USE_KAFKA)
using Momentum.Extensions.Messaging.Kafka;
//#endif
using Momentum.Extensions.Messaging.Wolverine;
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Messaging;
using Momentum.ServiceDefaults.HealthChecks;

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
builder.AddServiceBus(bus => bus.UseWolverine());
//#if (USE_KAFKA)
builder.AddKafkaMessagingExtensions();
//#endif
builder.AddOrleans();

builder.AddAppDomainServices();
builder.AddApplicationServices();

var app = builder.Build();

app.MapDashboard();
app.MapDefaultHealthCheckEndpoints();

await app.RunAsync(args);
