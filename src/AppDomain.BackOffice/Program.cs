// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.BackOffice;
using AppDomain.Infrastructure;
using Operations.ServiceDefaults;
using Operations.ServiceDefaults.HealthChecks;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

// Application Services
builder.AddAppDomainServices();
builder.AddApplicationServices();

var app = builder.Build();

app.MapDefaultHealthCheckEndpoints();

await app.RunAsync(args);
