// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Infrastructure;
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApiServices();

// Add domain services
builder.AddAppDomainServices();

var app = builder.Build();

app.UseServiceDefaults();
app.UseApiServices();

app.Run();