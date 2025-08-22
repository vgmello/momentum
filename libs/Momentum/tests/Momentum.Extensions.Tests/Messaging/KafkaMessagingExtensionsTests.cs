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
            ["ConnectionStrings:Messaging"] = "localhost:9092"
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

        // Verify services are registered
        var serviceDescriptor = builder.Services.FirstOrDefault(s =>
            s.ServiceType == typeof(IWolverineExtension) &&
            s.ImplementationType == typeof(KafkaEventsExtensions));

        serviceDescriptor.ShouldNotBeNull();
        serviceDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddKafkaMessagingExtensions_WithMissingConnectionString_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => builder.AddKafkaMessagingExtensions());

        exception.Message.ShouldBe("Kafka connection string 'Messaging' not found in configuration.");
    }

    [Fact]
    public void AddKafkaMessagingExtensions_WithEmptyConnectionString_ThrowsException()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Messaging"] = ""
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
            builder.AddKafkaMessagingExtensions());

        exception.Message.ShouldBe("Kafka connection string 'Messaging' not found in configuration.");
    }

    [Fact]
    public void AddKafkaMessagingExtensions_RegistersHealthChecks()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Messaging"] = "localhost:9092"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Act
        builder.AddKafkaMessagingExtensions();

        // Assert
        var healthCheckService = builder.Services.FirstOrDefault(s =>
            s.ServiceType.Name.Contains("HealthCheck"));

        healthCheckService.ShouldNotBeNull();
    }
}
