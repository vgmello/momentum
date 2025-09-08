// Copyright (c) OrgName. All rights reserved.

var builder = DistributedApplication.CreateBuilder(args);

#if (USE_PGSQL)
var dbPassword = builder.AddParameter("DbPassword", secret: true);

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
        .WithEndpointUrl("http", "PgAdmin (DB Management)"))
    .WithLifetime(ContainerLifetime.Persistent);

var database = pgsql.AddDatabase(name: "AppDomainDb", databaseName: "app_domain");
var serviceBusDb = pgsql.AddDatabase(name: "ServiceBus", databaseName: "service_bus");

#if (USE_LIQUIBASE)
var liquibaseMigrations = builder.AddLiquibaseMigrations(pgsql, dbPassword);
#endif

#endif
#if (USE_KAFKA)
var kafka = builder.AddKafka("Messaging", port: 59092);

kafka.WithKafkaUI(resource => resource
    .WithHostPort(port: 59093)
    .WaitFor(kafka)
    .WithEndpointUrl("http", "Kafka UI"));

#endif
#if (INCLUDE_ORLEANS)
var storage = builder.AddAzureStorage("app-domain-azure-storage").RunAsEmulator();
var clustering = storage.AddTables("Orleans");
var grainStorage = storage.AddBlobs("OrleansGrainState");

var orleans = builder
    .AddOrleans("app-domain-orleans")
    .WithClustering(clustering)
    .WithGrainStorage("Default", grainStorage);

#endif
#if (INCLUDE_API)
var appDomainApi = builder
    .AddProject<Projects.AppDomain_Api>("app-domain-api")
    .WithEnvironment("ServiceName", "AppDomain")
#if (INCLUDE_ORLEANS)
    .WithEnvironment("Aspire__Azure__Data__Tables__DisableHealthChecks", "true")
    .WithEnvironment("Orleans__UseLocalhostClustering", "false")
#endif
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
#if (USE_LIQUIBASE)
    .WaitFor(liquibaseMigrations)
#endif
#endif
    .WithEndpointUrl("http|https", "AppDomain API", "/scalar")
    .WithHealthCheck("/health/internal");

#endif
#if (INCLUDE_BACK_OFFICE)
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
#if (USE_LIQUIBASE)
    .WaitFor(liquibaseMigrations)
#endif
#endif
    .WithHealthCheck("/health/internal");

#endif
#if (INCLUDE_ORLEANS)
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
#if (USE_LIQUIBASE)
    .WaitFor(liquibaseMigrations)
#endif
#endif
    .WithReplicas(3)
    .WithEndpointUrl("http|https", "Dashboard", "/dashboard")
    .WithHealthCheck("/health/internal");

#endif
#if (INCLUDE_DOCS)
builder
    .AddContainer("app-domain-docs", "app-domain-docs")
    .WithDockerfile("../../", "docs/Dockerfile")
    .WithBindMount("../../", "/app")
    .WithVolume("/app/docs/node_modules")
    .WithHttpEndpoint(port: 8119, targetPort: 5173, name: "http")
    .WithEndpointUrl("http", "App Documentation")
#if (INCLUDE_API)
    .WaitFor(appDomainApi)
#endif
    .WithHttpHealthCheck("/");

#endif

// k6 Performance Testing Container
var k6Performance = builder
    .AddContainer("k6-performance", "k6-performance")
    .WithDockerfile("../../tests/performance/k6", "Dockerfile")
    .WithBindMount("../../tests/performance/k6", "/scripts")
    .WithBindMount("../../tests/performance/results", "/results")
    .WithHttpEndpoint(port: 5665, name: "web-dashboard")
    .WithEnvironment("ENVIRONMENT", builder.Configuration["Environment"] ?? "local")
    .WithEnvironment("ENABLE_KAFKA_VALIDATION", builder.Configuration["EnableKafkaValidation"] ?? "false")
    .WithEnvironment("K6_WEB_DASHBOARD", "true")
    .WithEnvironment("K6_WEB_DASHBOARD_EXPORT", "/results/web-dashboard-export.html")
#if (INCLUDE_API)
    .WithEnvironment("API_BASE_URL", appDomainApi.GetEndpoint("http"))
    .WithEnvironment("GRPC_ENDPOINT", appDomainApi.GetEndpoint("grpc"))
    .WaitFor(appDomainApi)
#endif
#if (INCLUDE_ORLEANS)
    .WithEnvironment("ORLEANS_URL", "http://localhost:8104")
#endif
#if (USE_KAFKA)
    .WithEnvironment("KAFKA_BOOTSTRAP_SERVERS", kafka.GetEndpoint("tcp"))
#endif
    .WithArgs("run", "--web-dashboard", "--web-dashboard-export=/results/web-dashboard-export.html", "--out", "json=/results/results.json", "/scripts/scenarios/mixed/realistic-workflow.js")
    .WithEndpointUrl("web-dashboard", "k6 Performance Dashboard");

await builder.Build().RunAsync();
