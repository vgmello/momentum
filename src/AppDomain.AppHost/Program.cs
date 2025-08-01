// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var dbPassword = builder.AddParameter("DbPassword", secret: true);

var pgsql = builder
    .AddPostgres("AppDomain-db", password: dbPassword, port: 54320)
    .WithImage("postgres", "17-alpine")
    .WithContainerName("AppDomain-db")
    .WithEndpointProxySupport(false)
    .WithPgAdmin(pgAdmin => pgAdmin
        .WithHostPort(port: 54321)
        .WithEndpointProxySupport(false)
        .WithImage("dpage/pgadmin4", "latest")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithUrlForEndpoint("http", url => url.DisplayText = "PgAdmin (DB Management)"))
    .WithLifetime(ContainerLifetime.Persistent);

var database = pgsql.AddDatabase(name: "AppDomainDb", databaseName: "AppDomain");
var serviceBusDb = pgsql.AddDatabase(name: "ServiceBus", databaseName: "service_bus");
builder.AddLiquibaseMigrations(pgsql, dbPassword);

var kafka = builder.AddKafka("messaging", port: 59092);
kafka.WithKafkaUI(resource => resource
    .WithHostPort(port: 59093)
    .WaitFor(kafka)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Kafka UI"));

var storage = builder.AddAzureStorage("AppDomain-azure-storage").RunAsEmulator();
var clustering = storage.AddTables("OrleansClustering");
var grainTables = storage.AddTables("OrleansGrainState");

var orleans = builder
    .AddOrleans("AppDomain-orleans")
    .WithClustering(clustering)
    .WithGrainStorage("Default", grainTables);

var AppDomainApi = builder
    .AddProject<Projects.AppDomain_Api>("AppDomain-api")
    .WithEnvironment("ServiceName", "AppDomain")
    .WithKestrelLaunchProfileEndpoints()
    .WithReference(database)
    .WithReference(serviceBusDb)
    .WithReference(kafka)
    .WaitFor(database)
    .WithHttpHealthCheck("/health/internal");

builder
    .AddProject<Projects.AppDomain_BackOffice>("AppDomain-backoffice")
    .WithEnvironment("ServiceName", "AppDomain")
    .WithReference(database)
    .WithReference(serviceBusDb)
    .WithReference(kafka)
    .WaitFor(database)
    .WithHttpHealthCheck("/health/internal");

builder
    .AddProject<Projects.AppDomain_BackOffice_Orleans>("AppDomain-backoffice-orleans")
    .WithEnvironment("ServiceName", "AppDomain")
    .WithEnvironment("Orleans__UseLocalhostClustering", "false")
    .WithEnvironment("Aspire__Azure__Data__Tables__DisableHealthChecks", "true")
    .WithReference(orleans)
    .WithReference(database)
    .WithReference(serviceBusDb)
    .WithReference(kafka)
    .WaitFor(pgsql)
    .WithReplicas(3)
    .WithUrlForEndpoint("https", url =>
    {
        url.DisplayText = "Dashboard";
        url.Url = "/dashboard";
    })
    .WithHttpHealthCheck("/health/internal");

builder
    .AddContainer("AppDomain-docs", "AppDomain-docfx")
    .WithDockerfile("../../", "docs/Dockerfile")
    .WithBindMount("../../", "/app")
    .WithVolume("/app/docs/node_modules")
    .WithHttpEndpoint(port: 8119, targetPort: 5173, name: "http")
    .WithUrlForEndpoint("http", url => url.DisplayText = "App Documentation")
    .WaitFor(AppDomainApi)
    .WithHttpHealthCheck("/");

// builder
//     .AddNpmApp("AppDomain-ui", "../../../AppDomain/web/AppDomain-ui", "dev")
//     .WithHttpEndpoint(env: "PORT", port: 8105, isProxied: false)

await builder.Build().RunAsync();
