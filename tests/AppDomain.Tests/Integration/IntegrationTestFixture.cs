// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Api;
using AppDomain.Tests.Integration._Internal;
using AppDomain.Tests.Integration._Internal.Containers;
using AppDomain.Tests.Integration._Internal.Extensions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;
using Momentum.ServiceDefaults.Messaging.Wolverine;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace AppDomain.Tests.Integration;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class IntegrationTestFixture : WebApplicationFactory<AppDomain.Api.Program>, IAsyncLifetime
{
    private readonly INetwork _containerNetwork = new NetworkBuilder().Build();

    private readonly PostgreSqlContainer _postgres;
    private readonly KafkaContainer _kafka;

    public GrpcChannel GrpcChannel { get; private set; } = null!;

    public ITestOutputHelper? TestOutput { get; set; }

    public IntegrationTestFixture()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithNetwork(_containerNetwork)
            .Build();

        _kafka = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:latest")
            .WithNetwork(_containerNetwork)
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _containerNetwork.CreateAsync();
        await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());

        await using var liquibaseMigrationContainer = new LiquibaseMigrationContainer(_postgres.Name, _containerNetwork);
        await liquibaseMigrationContainer.StartAsync();

        GrpcChannel = GrpcChannel.ForAddress(Server.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = Server.CreateHandler()
        });
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _kafka.DisposeAsync().AsTask());
        await _containerNetwork.DisposeAsync();
        await Log.CloseAndFlushAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:AppDomainDb", _postgres.GetDbConnectionString("app_domain"));
        builder.UseSetting("ConnectionStrings:ServiceBus", _postgres.GetDbConnectionString("service_bus"));
        builder.UseSetting("ConnectionStrings:Messaging", _kafka.GetBootstrapAddress());

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
}
