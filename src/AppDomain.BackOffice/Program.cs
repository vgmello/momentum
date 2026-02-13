// Copyright (c) OrgName. All rights reserved.

using AppDomain.BackOffice;
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

builder.AddAppDomainServices();
builder.AddApplicationServices();

var app = builder.Build();

app.MapDefaultHealthCheckEndpoints();

await app.RunAsync(args);
