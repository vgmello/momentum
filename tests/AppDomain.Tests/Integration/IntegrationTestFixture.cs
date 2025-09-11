// Copyright (c) OrgName. All rights reserved.

using AppDomain.Tests.Integration._Internal;
using AppDomain.Tests.Integration._Internal.Extensions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;
using System.Net;
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
using AppDomain.Api;
using Momentum.ServiceDefaults.Api;
//#endif
//#if (HAS_BACKEND)
using AppDomain.Infrastructure;
using Momentum.ServiceDefaults.HealthChecks;
//#endif
//#if (USE_KAFKA)
using Momentum.Extensions.Messaging.Kafka;
//#endif
//#if (INCLUDE_BACK_OFFICE)
using AppDomain.BackOffice;
//#endif
//#if (INCLUDE_ORLEANS)
using AppDomain.BackOffice.Orleans;
using AppDomain.BackOffice.Orleans.Infrastructure.Extensions;
//#endif
//#if (INCLUDE_API && INCLUDE_ORLEANS)

//#endif

[assembly: DomainAssembly(typeof(IAppDomainAssembly))]

namespace AppDomain.Tests.Integration;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly INetwork _containerNetwork = new NetworkBuilder().Build();

    //#if (HAS_BACKEND)
    private WebApplication? _app;
    //#endif

    //#if (USE_DB)
    private readonly PostgreSqlContainer _postgres;

    //#endif
    //#if (USE_KAFKA)
    private readonly KafkaContainer _kafka;
    //#endif

    //#if (INCLUDE_API)
    public GrpcChannel GrpcChannel { get; private set; } = null!;
    //#endif
    //#if (HAS_BACKEND)
    public IServiceProvider Services => _app?.Services ?? throw new InvalidOperationException("Application not initialized");
    //#endif

    //#if (USE_DB)
    public string AppDomainDbConnectionString => _postgres.GetDbConnectionString("app_domain");

    public string ServiceBusDbConnectionString => _postgres.GetDbConnectionString("service_bus");

    //#endif
    //#if (USE_KAFKA)
    public string KafkaBootstrapAddress => _kafka.GetBootstrapAddress();
    //#endif

    public ITestOutputHelper? TestOutput { get; set; }

    public IntegrationTestFixture()
    {
        // Enable HTTP/2 over unencrypted connections for gRPC testing
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        //#if (USE_DB)
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("postgres")
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
        //#if (USE_DB)
        await _postgres.StartAsync();
        //#endif
        //#if (USE_KAFKA)
        await _kafka.StartAsync();
        //#endif

        //#if (USE_LIQUIBASE)
        await using var liquibaseMigrationContainer = new LiquibaseMigrationContainer(_postgres.Name, _containerNetwork);
        await liquibaseMigrationContainer.StartAsync();
        //#endif

        //#if (HAS_BACKEND)
        await CreateTestWebApplicationAsync();
        //#endif
    }

    //#if (HAS_BACKEND)
    private async Task CreateTestWebApplicationAsync()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        var configData = new Dictionary<string, string?>
        {
            //#if (USE_DB)
            ["ConnectionStrings:AppDomainDb"] = _postgres.GetDbConnectionString("app_domain"),
            ["ConnectionStrings:ServiceBus"] = _postgres.GetDbConnectionString("service_bus")
            //#endif
        };

        //#if (USE_KAFKA)
        var kafkaAddress = _kafka.GetBootstrapAddress();
        configData["ConnectionStrings:Messaging"] = kafkaAddress;
        configData["Aspire:Confluent:Kafka:Messaging:BootstrapServers"] = kafkaAddress;
        configData["Aspire:Confluent:Kafka:Messaging:Consumer:Config:GroupId"] = "integration-test-group";
        configData["Aspire:Confluent:Kafka:Messaging:Consumer:Config:AutoOffsetReset"] = "Latest";
        configData["Aspire:Confluent:Kafka:Messaging:Consumer:Config:EnableAutoCommit"] = "true";
        configData["Aspire:Confluent:Kafka:Messaging:Security:Protocol"] = "Plaintext";

        //#endif
        configData["Orleans:UseLocalhostClustering"] = "true";
        configData["ServiceBus:Wolverine:CodegenEnabled"] = "true";

        builder.Configuration.AddInMemoryCollection(configData);

        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                //#if (INCLUDE_API)
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                //#else
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
                //#endif
            });
        });

        builder.Services.AddLogging(logging => logging
            .ClearProviders()
            .AddSerilog(CreateTestLogger(nameof(AppDomain)).ForContext("Integration", "Test")));

        ServiceDefaultsExtensions.EntryAssembly = typeof(IAppDomainAssembly).Assembly;

        builder.AddAppDomainServices();
        builder.AddServiceDefaults();
        //#if (USE_KAFKA)
        builder.AddKafkaMessagingExtensions();
        //#endif
        //#if (INCLUDE_API)
        builder.AddApiServiceDefaults();
        Api.DependencyInjection.AddApplicationServices(builder);
        //#endif
        //#if (INCLUDE_BACKOFFICE)
        BackOffice.DependencyInjection.AddApplicationServices(builder);
        //#if (INCLUDE_ORLEANS)
        builder.AddOrleans();
        BackOffice.Orleans.DependencyInjection.AddApplicationServices(builder);
        //#endif

        _app = builder.Build();

        //#if (INCLUDE_API)
        _app.ConfigureApiUsingDefaults(requireAuth: false);
        _app.MapGrpcServices(typeof(Program));
        //#endif

        await _app.StartAsync();

        //#if (INCLUDE_API)
        var httpClient = new HttpClient(new HttpClientHandler())
        {
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };

        GrpcChannel = GrpcChannel.ForAddress(_app.Urls.First(), new GrpcChannelOptions
        {
            HttpClient = httpClient
        });
        //#endif
    }
    //#endif

    public async ValueTask DisposeAsync()
    {
        //#if (HAS_BACKEND)
        if (_app != null)
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
    private Logger CreateTestLogger(string logNamespace) =>
        new LoggerConfiguration()
            .WriteTo.Sink(new XUnitSink(() => TestOutput))
            .MinimumLevel.Information()
            .MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Warning)
            .MinimumLevel.Override("LinqToDB", LogEventLevel.Debug)
            .MinimumLevel.Override("AppDomain", LogEventLevel.Debug)
            .MinimumLevel.Override(logNamespace, LogEventLevel.Debug)
            .CreateLogger();
    //#endif
}
