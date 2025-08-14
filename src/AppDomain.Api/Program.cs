// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Infrastructure;
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApiServices();

// Add domain services
builder.AddAppDomainServices();

//#if (USE_KAFKA)
builder.Services.AddKafkaMessaging(builder.Configuration);
//#endif

var app = builder.Build();

app.UseServiceDefaults();
app.UseApiServices();

app.Run();
