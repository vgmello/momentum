// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Tests.Integration._Internal;
using AppDomain.Tests.Integration._Internal.Extensions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Messaging.Wolverine;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;
#if USE_DB
using AppDomain.Tests.Integration._Internal.Containers;
using Testcontainers.PostgreSql;
#endif
#if USE_KAFKA
using Testcontainers.Kafka;
#endif
#if INCLUDE_API
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Momentum.ServiceDefaults.Api;
#endif

namespace AppDomain.Tests.Integration;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
#if INCLUDE_API
public class IntegrationTestFixture : WebApplicationFactory<AppDomain.Api.Program>, IAsyncLifetime
#else
public class IntegrationTestFixture : IAsyncLifetime
#endif
{
    private readonly INetwork _containerNetwork = new NetworkBuilder().Build();

#if USE_DB
    private readonly PostgreSqlContainer _postgres;
#endif
#if USE_KAFKA
    private readonly KafkaContainer _kafka;
#endif

#if INCLUDE_API
    public GrpcChannel GrpcChannel { get; private set; } = null!;
#endif

#if USE_DB
    public string AppDomainDbConnectionString => _postgres.GetDbConnectionString("app_domain");
    public string ServiceBusDbConnectionString => _postgres.GetDbConnectionString("service_bus");
#endif
#if USE_KAFKA
    public string KafkaBootstrapAddress => _kafka.GetBootstrapAddress();
#endif

    public ITestOutputHelper? TestOutput { get; set; }

    public IntegrationTestFixture()
    {
#if USE_DB
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithNetwork(_containerNetwork)
            .Build();
#endif

#if USE_KAFKA
        _kafka = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.6.0")
            .WithNetwork(_containerNetwork)
            .Build();
#endif
    }

    public async ValueTask InitializeAsync()
    {
        await _containerNetwork.CreateAsync();
#if USE_DB
        await _postgres.StartAsync();
#endif
#if USE_KAFKA
        await _kafka.StartAsync();
#endif

#if USE_DB
        await using var liquibaseMigrationContainer = new LiquibaseMigrationContainer(_postgres.Name, _containerNetwork);
        await liquibaseMigrationContainer.StartAsync();
#endif

#if INCLUDE_API
        GrpcChannel = GrpcChannel.ForAddress(Server.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = Server.CreateHandler()
        });
#endif
    }

#if INCLUDE_API
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        var disposeTasks = new List<Task>();
#if USE_DB
        disposeTasks.Add(_postgres.DisposeAsync().AsTask());
#endif
#if USE_KAFKA
        disposeTasks.Add(_kafka.DisposeAsync().AsTask());
#endif
        if (disposeTasks.Count > 0)
            await Task.WhenAll(disposeTasks);
        await _containerNetwork.DisposeAsync();
        await Log.CloseAndFlushAsync();
    }
#else
    public async ValueTask DisposeAsync()
    {
        var disposeTasks = new List<Task>();
#if USE_DB
        disposeTasks.Add(_postgres.DisposeAsync().AsTask());
#endif
#if USE_KAFKA
        disposeTasks.Add(_kafka.DisposeAsync().AsTask());
#endif
        if (disposeTasks.Count > 0)
            await Task.WhenAll(disposeTasks);
        await _containerNetwork.DisposeAsync();
        await Log.CloseAndFlushAsync();
    }
#endif

#if INCLUDE_API
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
#if USE_DB
        builder.UseSetting("ConnectionStrings:AppDomainDb", _postgres.GetDbConnectionString("app_domain"));
        builder.UseSetting("ConnectionStrings:ServiceBus", _postgres.GetDbConnectionString("service_bus"));
#endif
#if USE_KAFKA
        builder.UseSetting("ConnectionStrings:Messaging", _kafka.GetBootstrapAddress());
#endif
        builder.UseSetting("Orleans:UseLocalhostClustering", "true");

        WolverineSetupExtensions.SkipServiceRegistration = true;
        ServiceDefaultsExtensions.EntryAssembly = typeof(AppDomain.Api.Program).Assembly;

        builder.ConfigureServices((ctx, services) =>
        {
            services.RemoveServices<IHostedService>();
            services.RemoveServices<ILoggerFactory>();

            services.AddLogging(logging => logging
                .ClearProviders()
                .AddSerilog(CreateTestLogger(nameof(AppDomain))));

            services.AddWolverineWithDefaults(ctx.HostingEnvironment, ctx.Configuration,
                opt => opt.ApplicationAssembly = typeof(AppDomain.Api.Program).Assembly);
        });

        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapGrpcServices(typeof(AppDomain.Api.Program)));
        });
    }

    private Logger CreateTestLogger(string logNamespace) =>
        new LoggerConfiguration()
            .WriteTo.Sink(new XUnitSink(() => TestOutput))
            .MinimumLevel.Warning()
            .MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Warning)
            .MinimumLevel.Override(logNamespace, LogEventLevel.Verbose)
            .CreateLogger();
#endif
}
