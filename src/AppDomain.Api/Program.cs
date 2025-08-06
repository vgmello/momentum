// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Infrastructure;
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;
using Momentum.ServiceDefaults.HealthChecks;

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
builder.AddApiServiceDefaults();

// Application Services
builder.AddAppDomainServices();
builder.AddApplicationServices();

var app = builder.Build();

app.ConfigureApiUsingDefaults(requireAuth: false);
app.MapDefaultHealthCheckEndpoints();

await app.RunAsync(args);
