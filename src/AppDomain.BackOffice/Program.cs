// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.BackOffice;
using AppDomain.Infrastructure;
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.HealthChecks;

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

// Application Services
builder.AddAppDomainServices();
builder.AddApplicationServices();

var app = builder.Build();

app.MapDefaultHealthCheckEndpoints();

await app.RunAsync(args);
