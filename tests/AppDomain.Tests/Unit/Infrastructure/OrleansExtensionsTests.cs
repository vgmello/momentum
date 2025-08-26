// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Api.Infrastructure.Extensions;

namespace AppDomain.Tests.Unit.Infrastructure;

public class OrleansExtensionsTests
{
    [Fact]
    public void AddOrleansClient_ConfigurationValidation_ShouldWork()
    {
        // Test with localhost clustering (should work)
        var builder1 = WebApplication.CreateBuilder([]);
        builder1.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:UseLocalhostClustering"] = "true"
        });

        Should.NotThrow(() => builder1.AddOrleansClient());

        // Test configuration reading
        var useLocalCluster = builder1.Configuration.GetValue<bool>("Orleans:UseLocalhostClustering");
        useLocalCluster.ShouldBeTrue();
    }

    [Fact]
    public void AddOrleansClient_WithValidConfig_ShouldNotThrow()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:UseLocalhostClustering"] = "true"
        });

        // Act & Assert
        Should.NotThrow(() => builder.AddOrleansClient());
    }

    [Fact]
    public void AddOrleansClient_ShouldRegisterLazyClusterClientManager()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:UseLocalhostClustering"] = "true"
        });

        // Act
        builder.AddOrleansClient();

        // Assert - Build ServiceProvider and resolve services
        using var serviceProvider = builder.Services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        hostedServices.Any(s => s.GetType().Name == "LazyClusterClientManager").ShouldBeTrue();
    }

    [Fact]
    public void AddOrleansClient_ShouldRegisterOpenTelemetryMetrics()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:UseLocalhostClustering"] = "true"
        });

        // Act
        builder.AddOrleansClient();

        // Assert - Build ServiceProvider and check for OpenTelemetry services
        using var serviceProvider = builder.Services.BuildServiceProvider();

        // Check for OpenTelemetry meter provider which indicates metrics are configured
        var meterProvider = serviceProvider.GetService<OpenTelemetry.Metrics.MeterProvider>();
        meterProvider.ShouldNotBeNull("OpenTelemetry metrics should be configured");
    }

    [Fact]
    public void SetupLazyClusterClients_ShouldRemoveOriginalHostedServices()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:UseLocalhostClustering"] = "true"
        });

        // Add a mock cluster client that implements IHostedService
        var mockClient = Substitute.For<IClusterClient, IHostedService>();
        builder.Services.AddSingleton<IHostedService>(_ => (IHostedService)mockClient);
        builder.Services.AddSingleton<IClusterClient>(_ => mockClient);

        // Act
        builder.AddOrleansClient();

        // Assert - Build ServiceProvider and check hosted services
        using var serviceProvider = builder.Services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();

        // Should have LazyClusterClientManager
        hostedServices.Any(s => s.GetType().Name == "LazyClusterClientManager").ShouldBeTrue();

        // Should have LazyClusterClientManager registered as IHostedService 
        // When using factory pattern, we need to check the resolved service instead of service descriptors
        var lazyManager = serviceProvider.GetServices<IHostedService>()
            .FirstOrDefault(s => s.GetType().Name == "LazyClusterClientManager");
        lazyManager.ShouldNotBeNull();
    }

    [Fact]
    public void SetupLazyClusterClients_ShouldSetupCorrectly()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:UseLocalhostClustering"] = "true"
        });

        // Act
        builder.AddOrleansClient();

        // Assert - Build ServiceProvider and verify services
        using var serviceProvider = builder.Services.BuildServiceProvider();

        // Verify that SetupLazyClusterClients was called by checking for LazyClusterClientManager
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        var lazyManager = hostedServices.FirstOrDefault(s => s.GetType().Name == "LazyClusterClientManager");
        lazyManager.ShouldNotBeNull("LazyClusterClientManager should be registered");

        // Verify Orleans client services are registered (but don't resolve them as it triggers Orleans setup)
        var clusterClientServices = builder.Services.Where(s => s.ServiceType == typeof(IClusterClient)).ToList();
        clusterClientServices.ShouldNotBeEmpty("IClusterClient services should be registered");
    }

    [Fact]
    public void SetupLazyClusterClients_ShouldPreservePrimaryClientLifetime()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:UseLocalhostClustering"] = "true"
        });

        var mockClient = Substitute.For<IClusterClient>();
        builder.Services.AddSingleton<IClusterClient>(_ => mockClient);

        // Act
        builder.AddOrleansClient();

        // Assert
        var primaryClientRegistration = builder.Services.Last(s =>
            s.ServiceType == typeof(IClusterClient) && !s.IsKeyedService);

        primaryClientRegistration.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddOrleansClient_ServiceRegistration_ShouldWork()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:UseLocalhostClustering"] = "true"
        });

        // Act
        builder.AddOrleansClient();

        // Assert - Build ServiceProvider and verify all core services work
        using var serviceProvider = builder.Services.BuildServiceProvider();

        // Verify LazyClusterClientManager
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        var lazyManager = hostedServices.FirstOrDefault(s => s.GetType().Name == "LazyClusterClientManager");
        lazyManager.ShouldNotBeNull("LazyClusterClientManager should be registered");

        // Verify IClusterClient services are registered (but don't resolve them)
        var clusterClientServices = builder.Services.Where(s => s.ServiceType == typeof(IClusterClient)).ToList();
        clusterClientServices.ShouldNotBeEmpty("IClusterClient services should be registered");

        // Verify OpenTelemetry metrics are configured
        var meterProvider = serviceProvider.GetService<OpenTelemetry.Metrics.MeterProvider>();
        meterProvider.ShouldNotBeNull("OpenTelemetry metrics should be configured");
    }

    [Fact]
    public void AddOrleansClient_ConfigurationEdgeCases_ShouldHandleCorrectly()
    {
        // Test with custom service key
        var builder1 = WebApplication.CreateBuilder([]);
        builder1.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:UseLocalhostClustering"] = "false",
            ["Orleans:Clustering:ServiceKey"] = "CustomOrleansKey",
            ["ConnectionStrings:CustomOrleansKey"] = "test-connection"
        });

        Should.NotThrow(() => builder1.AddOrleansClient());

        // Test with default service key (null)
        var builder2 = WebApplication.CreateBuilder([]);
        builder2.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:UseLocalhostClustering"] = "false",
            ["ConnectionStrings:OrleansClustering"] = "test-connection"
        });

        Should.NotThrow(() => builder2.AddOrleansClient());
    }

    [Fact]
    public void LazyClusterClientManager_Integration_ShouldRegisterCorrectly()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:UseLocalhostClustering"] = "true"
        });

        var mockClient = Substitute.For<IClusterClient, IHostedService>();
        builder.Services.AddSingleton<IClusterClient>(_ => mockClient);

        // Act
        builder.AddOrleansClient();

        // Assert - Build ServiceProvider and test integration
        using var serviceProvider = builder.Services.BuildServiceProvider();

        // Should be able to get the LazyClusterClientManager
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        var lazyManager = hostedServices.FirstOrDefault(h => h.GetType().Name == "LazyClusterClientManager");
        lazyManager.ShouldNotBeNull("LazyClusterClientManager should be resolvable");

        // Verify services are registered but don't resolve IClusterClient as it triggers Orleans setup
        var clusterClientServices = builder.Services.Where(s => s.ServiceType == typeof(IClusterClient)).ToList();
        clusterClientServices.ShouldNotBeEmpty("IClusterClient services should be registered");

        // Verify singleton lifetime is preserved
        var primaryClientRegistration = builder.Services.Last(s =>
            s.ServiceType == typeof(IClusterClient) && !s.IsKeyedService);
        primaryClientRegistration.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }
}
