// Copyright (c) ORG_NAME. All rights reserved.

///<summary>
/// AppDomain Back Office Service
/// 
/// Provides asynchronous background processing for the AppDomain application.
/// This service handles event-driven operations, message processing, and other
/// background tasks that don't require immediate synchronous responses.
/// 
/// Key responsibilities:
/// - Processing integration events from Kafka
/// - Handling long-running background operations
/// - Managing asynchronous business workflows
/// </summary>

using AppDomain.BackOffice;
using AppDomain.Infrastructure;
using Momentum.Extensions.Messaging.Kafka;
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
