// Copyright (c) OrgName. All rights reserved.

using AppDomain.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

#if (db == npgsql)
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

var database = pgsql.AddDatabase(name: "AppDomainDb", databaseName: "app_domain");
var serviceBusDb = pgsql.AddDatabase(name: "ServiceBus", databaseName: "service_bus");
#if (db == liquibase)
builder.AddLiquibaseMigrations(pgsql, dbPassword);
#endif

#endif
#if (useKafka)
var kafka = builder.AddKafka("messaging", port: 59092);
kafka.WithKafkaUI(resource => resource
    .WithHostPort(port: 59093)
    .WaitFor(kafka)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Kafka UI"));

#endif
#if (includeOrleans)
var storage = builder.AddAzureStorage("AppDomain-azure-storage").RunAsEmulator();
var clustering = storage.AddTables("OrleansClustering");
var grainTables = storage.AddTables("OrleansGrainState");

var orleans = builder
    .AddOrleans("AppDomain-orleans")
    .WithClustering(clustering)
    .WithGrainStorage("Default", grainTables);

#endif
#if (includeApi)
var AppDomainApi = builder
    .AddProject<Projects.AppDomain_Api>("AppDomain-api")
    .WithEnvironment("ServiceName", "AppDomain")
    .WithKestrelLaunchProfileEndpoints()
#if (useDb)
    .WithReference(database)
    .WithReference(serviceBusDb)
#endif
    .WithReference(kafka)
#if (useDb)
    .WaitFor(database)
#endif
    .WithHttpHealthCheck("/health/internal");

#endif
#if (includeBackOffice)
builder
    .AddProject<Projects.AppDomain_BackOffice>("AppDomain-backoffice")
    .WithEnvironment("ServiceName", "AppDomain")
#if (useDb)
    .WithReference(database)
    .WithReference(serviceBusDb)
#endif
    .WithReference(kafka)
#if (useDb)
    .WaitFor(database)
#endif
    .WithHttpHealthCheck("/health/internal");

#endif
#if (includeOrleans)
builder
    .AddProject<Projects.AppDomain_BackOffice_Orleans>("AppDomain-backoffice-orleans")
    .WithEnvironment("ServiceName", "AppDomain")
    .WithEnvironment("Orleans__UseLocalhostClustering", "false")
    .WithEnvironment("Aspire__Azure__Data__Tables__DisableHealthChecks", "true")
    .WithReference(orleans)
#if (useDb)
    .WithReference(database)
    .WithReference(serviceBusDb)
#endif
    .WithReference(kafka)
#if (useDb)
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
builder
    .AddContainer("AppDomain-docs", "AppDomain-docfx")
    .WithDockerfile("../../", "docs/Dockerfile")
    .WithBindMount("../../", "/app")
    .WithVolume("/app/docs/node_modules")
    .WithHttpEndpoint(port: documentationHttp, targetPort: 5173, name: "http")
    .WithUrlForEndpoint("http", url => url.DisplayText = "App Documentation")
#if (includeApi)
    .WaitFor(AppDomainApi)
#endif
    .WithHttpHealthCheck("/");

await builder.Build().RunAsync();
