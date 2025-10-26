// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Momentum.Extensions.Messaging.Kafka;
using Wolverine;

namespace Momentum.Extensions.Tests.Messaging;

public class KafkaMessagingExtensionsTests
{
    [Fact]
    public void AddKafkaMessagingExtensions_WithValidConfiguration_RegistersServices()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "localhost:9092"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Act
        var result = builder.AddKafkaMessagingExtensions();

        // Assert
        result.ShouldBe(builder);

        // Verify Aspire Kafka services are registered (they may be keyed services)
        var aspireServices = builder.Services.Where(s => 
            s.ServiceType.Name.Contains("Kafka") || 
            s.ServiceType.Namespace?.Contains("Confluent") == true ||
            s.ServiceType.Namespace?.Contains("Aspire") == true).ToList();
        
        // Should have some Aspire-related Kafka services registered
        aspireServices.ShouldNotBeEmpty();

        // Verify Wolverine extension is registered
        var wolverineExtension = builder.Services.FirstOrDefault(s =>
            s.ServiceType == typeof(IWolverineExtension));
        wolverineExtension.ShouldNotBeNull();
        wolverineExtension.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddKafkaMessagingExtensions_WithMissingConnectionString_DoesNotThrow()
    {
        // Arrange - Aspire handles missing connection strings gracefully
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Act - Should not throw as Aspire will handle configuration at runtime
        Should.NotThrow(() => builder.AddKafkaMessagingExtensions());
        
        // Verify services are still registered
        var wolverineExtension = builder.Services.FirstOrDefault(s =>
            s.ServiceType == typeof(IWolverineExtension));
        wolverineExtension.ShouldNotBeNull();
    }

    [Fact]
    public void AddKafkaMessagingExtensions_WithCustomConnectionName_RegistersServices()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:custom-kafka"] = "localhost:9092"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Act
        builder.AddKafkaMessagingExtensions("custom-kafka");

        // Assert - Services should be registered regardless of connection name
        var wolverineExtension = builder.Services.FirstOrDefault(s =>
            s.ServiceType == typeof(IWolverineExtension));
        wolverineExtension.ShouldNotBeNull();
    }

    [Fact]
    public void AddKafkaMessagingExtensions_WithProducerConsumerConfig_RegistersServices()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "localhost:9092"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Act
        builder.AddKafkaMessagingExtensions(
            configureProducerSettings: settings => { /* custom producer config */ },
            configureConsumerSettings: settings => { /* custom consumer config */ });

        // Assert - Health checks are automatically registered by Aspire
        var wolverineExtension = builder.Services.FirstOrDefault(s =>
            s.ServiceType == typeof(IWolverineExtension));
        wolverineExtension.ShouldNotBeNull();
    }
}
