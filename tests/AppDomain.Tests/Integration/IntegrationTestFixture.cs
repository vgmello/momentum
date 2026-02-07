// Copyright (c) OrgName. All rights reserved.

using AppDomain.Tests.Integration._Internal;
using AppDomain.Tests.Integration._Internal.Extensions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Momentum.ServiceDefaults;
using AppDomain;
//#if (USE_LIQUIBASE)
using AppDomain.Tests.Integration._Internal.Containers;
//#endif
//#if (USE_DB)
using Testcontainers.PostgreSql;
//#endif
//#if (USE_KAFKA)
using Testcontainers.Kafka;
//#endif
//#if (INCLUDE_API)
using Grpc.Net.Client;
using Momentum.ServiceDefaults.Api;
//#endif
//#if (HAS_BACKEND)
using AppDomain.Infrastructure;
//#endif
//#if (USE_KAFKA)
using Momentum.Extensions.Messaging.Kafka;
//#endif
//#if (INCLUDE_ORLEANS)
using AppDomain.BackOffice.Orleans.Infrastructure.Extensions;
using Microsoft.AspNetCore.TestHost;
//#endif

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

namespace AppDomain.Tests.Integration;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly INetwork _containerNetwork = new NetworkBuilder().Build();

    //#if (HAS_BACKEND)
    private WebApplication? _app;

    public WebApplication App => _app ?? throw new InvalidOperationException("Application not initialized");
    public IServiceProvider Services => _app?.Services ?? throw new InvalidOperationException("Application not initialized");

    //#endif
    //#if (USE_DB)
    private readonly PostgreSqlContainer _postgres;

    public string AppDomainDbConnectionString => _postgres.GetDbConnectionString("app_domain");

    public string ServiceBusDbConnectionString => _postgres.GetDbConnectionString("service_bus");

    //#endif
    //#if (USE_KAFKA)
    private readonly KafkaContainer _kafka;

    public string KafkaBootstrapAddress => _kafka.GetBootstrapAddress();

    //#endif
    //#if (INCLUDE_API)
    public GrpcChannel GrpcChannel { get; private set; } = null!;

    //#endif
    public ITestOutputHelper? TestOutput { get; set; }

    public IntegrationTestFixture()
    {
        //#if (USE_DB)
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithNetwork(_containerNetwork)
            .Build();

        //#endif
        //#if (USE_KAFKA)
        _kafka = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.6.0")
            .WithNetwork(_containerNetwork)
            .Build();
        //#endif
    }

    public async ValueTask InitializeAsync()
    {
        await _containerNetwork.CreateAsync();

        var containersTasks = new List<Task>
        {
            //#if (USE_DB && USE_LIQUIBASE)
            _postgres.StartAsync().ContinueWith(async _ =>
            {
                await using var liquibaseMigrationContainer = new LiquibaseMigrationContainer(_postgres.Name, _containerNetwork);
                await liquibaseMigrationContainer.StartAsync();
            }).Unwrap(),
            //#endif
            //#if (USE_DB && !USE_LIQUIBASE)
            _postgres.StartAsync(),
            //#endif
            //#if (USE_KAFKA)
            _kafka.StartAsync()
            //#endif
        };

        await Task.WhenAll(containersTasks);

        //#if (HAS_BACKEND)
        await CreateTestWebApplicationAsync();
        //#endif
    }

    //#if (HAS_BACKEND)
    private async Task CreateTestWebApplicationAsync()
    {
        ServiceDefaultsExtensions.EntryAssembly = typeof(IAppDomainAssembly).Assembly;

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        var configData = new Dictionary<string, string?>
        {
            //#if (USE_DB)
            ["ConnectionStrings:AppDomainDb"] = _postgres.GetDbConnectionString("app_domain"),
            ["ConnectionStrings:ServiceBus"] = _postgres.GetDbConnectionString("service_bus"),
            //#endif
            ["Orleans:UseLocalhostClustering"] = "true",
            ["ServiceBus:Wolverine:CodegenEnabled"] = "true",
            //#if (USE_KAFKA)
            ["ConnectionStrings:Messaging"] = _kafka.GetBootstrapAddress(),
            ["Aspire:Confluent:Kafka:Messaging:BootstrapServers"] = _kafka.GetBootstrapAddress(),
            ["Aspire:Confluent:Kafka:Messaging:Consumer:Config:GroupId"] = "integration-test-group",
            ["Aspire:Confluent:Kafka:Messaging:Consumer:Config:AutoOffsetReset"] = "Latest",
            ["Aspire:Confluent:Kafka:Messaging:Consumer:Config:EnableAutoCommit"] = "true",
            ["Aspire:Confluent:Kafka:Messaging:Security:Protocol"] = "Plaintext"
            //#endif
        };

        builder.Configuration.AddInMemoryCollection(configData);
        builder.WebHost.UseTestServer();

        builder.AddServiceDefaults();
        //#if (INCLUDE_API)
        builder.AddApiServiceDefaults();
        //#endif
        //#if (USE_KAFKA)
        builder.AddKafkaMessagingExtensions();
        //#endif

        builder.AddAppDomainServices();
        //#if (INCLUDE_API)
        Api.DependencyInjection.AddApplicationServices(builder);
        //#endif
        //#if (INCLUDE_BACKOFFICE)
        BackOffice.DependencyInjection.AddApplicationServices(builder);
        //#endif
        //#if (INCLUDE_ORLEANS)
        BackOffice.Orleans.DependencyInjection.AddApplicationServices(builder);

        builder.AddOrleans();
        //#endif

        builder.Services.AddSerilog(CreateTestLogger(builder.Configuration));

        _app = builder.Build();

        //#if (INCLUDE_API)
        _app.ConfigureApiUsingDefaults(requireAuth: false);
        _app.MapGrpcServices(typeof(Api.Program));
        //#endif

        await _app.StartAsync();

        //#if (INCLUDE_API)
        GrpcChannel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpClient = _app.GetTestClient()
        });
        //#endif
    }
    //#endif

    public async ValueTask DisposeAsync()
    {
        //#if (HAS_BACKEND)
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
        //#endif

        //#if (INCLUDE_API)
        GrpcChannel.Dispose();
        //#endif

        var disposeTasks = new List<Task>
        {
            //#if (USE_DB)
            _postgres.DisposeAsync().AsTask(),
            //#endif
            //#if (USE_KAFKA)
            _kafka.DisposeAsync().AsTask()
            //#endif
        };

        if (disposeTasks.Count > 0)
            await Task.WhenAll(disposeTasks);

        await _containerNetwork.DisposeAsync();
        await Log.CloseAndFlushAsync();
    }

    //#if (HAS_BACKEND)
    private Logger CreateTestLogger(IConfiguration configuration) =>
        new LoggerConfiguration()
            .WriteTo.Sink(new XUnitSink(() => TestOutput))
            .MinimumLevel.Warning()
            .MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Information)
            .MinimumLevel.Override("LinqToDB", LogEventLevel.Debug)
            .MinimumLevel.Override("AppDomain", LogEventLevel.Debug)
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    //#endif
}
