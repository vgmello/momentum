// Copyright (c) ORG_NAME. All rights reserved.

/// <summary>
/// .NET Aspire orchestration host for AppDomain microservices.
/// Configures and manages the complete application stack including databases,
/// messaging infrastructure, and service dependencies for local development.
/// </summary>

// Create the distributed application builder for orchestrating services

var builder = DistributedApplication.CreateBuilder(args);

#if (USE_PGSQL)
// Configure PostgreSQL database with secure password parameter
var dbPassword = builder.AddParameter("DbPassword", secret: true);

// Add PostgreSQL server with PgAdmin management UI
var pgsql = builder
    .AddPostgres("app-domain-db", password: dbPassword, port: 54320)
    .WithImage("postgres", "17-alpine")
    .WithContainerName("app-domain-db")
    .WithEndpointProxySupport(false)
    .WithPgAdmin(pgAdmin => pgAdmin
        .WithHostPort(port: 54321)
        .WithEndpointProxySupport(false)
        .WithImage("dpage/pgadmin4", "latest")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithUrlForEndpoint("http", url => url.DisplayText = "PgAdmin (DB Management)"))
    .WithLifetime(ContainerLifetime.Persistent);

// Create application and service bus databases
var database = pgsql.AddDatabase(name: "AppDomainDb", databaseName: "app_domain");
var serviceBusDb = pgsql.AddDatabase(name: "ServiceBus", databaseName: "service_bus");

#if (USE_LIQUIBASE)
// Configure database migrations using Liquibase
builder.AddLiquibaseMigrations(pgsql, dbPassword);
#endif

#endif
#if (USE_KAFKA)
// Add Apache Kafka for event streaming and messaging
var kafka = builder.AddKafka("messaging", port: 59092);

// Configure Kafka UI for message monitoring and management
kafka.WithKafkaUI(resource => resource
    .WithHostPort(port: 59093)
    .WaitFor(kafka)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Kafka UI"));

#endif
#if (INCLUDE_ORLEANS)
// Configure Azure Storage emulator for Orleans clustering and grain persistence
var storage = builder.AddAzureStorage("app-domain-azure-storage").RunAsEmulator();
var clustering = storage.AddTables("OrleansClustering");
var grainStorage = storage.AddBlobs("OrleansGrainState");

// Add Orleans for stateful actor-based processing
var orleans = builder
    .AddOrleans("app-domain-orleans")
    .WithClustering(clustering)
    .WithGrainStorage("Default", grainStorage);

#endif
#if (INCLUDE_API)
// Configure the main API service with REST and gRPC endpoints
var app_domainApi = builder
    .AddProject<Projects.AppDomain_Api>("app-domain-api")
    .WithEnvironment("ServiceName", "AppDomain")
    .WithKestrelLaunchProfileEndpoints()
#if (USE_DB)
    .WithReference(database)
    .WithReference(serviceBusDb)
#endif
#if (USE_KAFKA)
    .WithReference(kafka)
#endif
#if (INCLUDE_ORLEANS)
    .WithReference(orleans.AsClient())
#endif
#if (USE_DB)
    .WaitFor(database)
#endif
    .WithHttpHealthCheck("/health/internal");

#endif
#if (INCLUDE_BACK_OFFICE)
// Configure the back office service for asynchronous event processing
builder
    .AddProject<Projects.AppDomain_BackOffice>("app-domain-backoffice")
    .WithEnvironment("ServiceName", "AppDomain")
#if (USE_DB)
    .WithReference(database)
    .WithReference(serviceBusDb)
#endif
#if (USE_KAFKA)
    .WithReference(kafka)
#endif
#if (USE_DB)
    .WaitFor(database)
#endif
    .WithHttpHealthCheck("/health/internal");

#endif
#if (INCLUDE_ORLEANS)
// Configure Orleans-based back office service with clustering and grain storage
builder
    .AddProject<Projects.AppDomain_BackOffice_Orleans>("app-domain-backoffice-orleans")
    .WithEnvironment("ServiceName", "AppDomain")
    .WithEnvironment("Orleans__UseLocalhostClustering", "false")
    .WithEnvironment("Aspire__Azure__Data__Tables__DisableHealthChecks", "true")
    .WithReference(orleans)
#if (USE_DB)
    .WithReference(database)
    .WithReference(serviceBusDb)
#endif
#if (USE_KAFKA)
    .WithReference(kafka)
#endif
#if (USE_DB)
    .WaitFor(pgsql)
#endif
    .WithReplicas(3)
    .WithUrlForEndpoint("https", url =>
    {
        url.DisplayText = "Dashboard";
        url.Url = "/dashboard";
    })
    .WithHttpHealthCheck("/health/internal");

#endif
#if (INCLUDE_DOCS)
// Configure VitePress documentation container for developer documentation
builder
    .AddContainer("app-domain-docs", "app-domain-docs")
    .WithDockerfile("../../", "docs/Dockerfile")
    .WithBindMount("../../", "/app")
    .WithVolume("/app/docs/node_modules")
    .WithHttpEndpoint(port: 8119, targetPort: 5173, name: "http")
    .WithUrlForEndpoint("http", url => url.DisplayText = "App Documentation")
#if (INCLUDE_API)
    .WaitFor(app_domainApi)
#endif
    .WithHttpHealthCheck("/");

#endif

// Build and run the distributed application with all configured services
await builder.Build().RunAsync();
