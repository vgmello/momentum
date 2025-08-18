// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Infrastructure;
using Momentum.Extensions.Messaging.Kafka;
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;
using Momentum.ServiceDefaults.HealthChecks;

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

// Program entry point for the AppDomain API service.
// Configures and starts a web application with REST and gRPC endpoints,
// including service defaults, Kafka messaging, health checks, and domain services.

// Create a slim web application builder with the provided command-line arguments
var builder = WebApplication.CreateSlimBuilder(args);

// Configure service defaults (logging, telemetry, configuration)
builder.AddServiceDefaults();
// Configure API-specific defaults (Swagger, controllers, gRPC)
builder.AddApiServiceDefaults();
//#if (USE_KAFKA)
// Add Kafka messaging extensions for event publishing and consuming
builder.AddKafkaMessagingExtensions();
//#endif

// Configure domain and application services
builder.AddAppDomainServices(); // Database, data access, and domain infrastructure
builder.AddApplicationServices(); // API-specific application services

// Build the configured web application
var app = builder.Build();

// Configure the API pipeline with default middleware (no authentication required)
app.ConfigureApiUsingDefaults(requireAuth: false);
// Map standard health check endpoints for monitoring
app.MapDefaultHealthCheckEndpoints();

// Start the web application
await app.RunAsync(args);
