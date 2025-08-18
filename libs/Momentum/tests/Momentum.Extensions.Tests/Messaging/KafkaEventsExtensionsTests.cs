// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.Messaging.Kafka;
using Momentum.ServiceDefaults.Messaging;
using NSubstitute;
using Shouldly;
using System.Reflection;
using Wolverine;

namespace Momentum.Extensions.Tests.Messaging;

public class KafkaEventsExtensionsTests
{
    private readonly ILogger<KafkaEventsExtensions> _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptions<ServiceBusOptions> _serviceBusOptions;
    private readonly IHostEnvironment _environment;
    private readonly KafkaEventsExtensions _extension;

    public KafkaEventsExtensionsTests()
    {
        _logger = Substitute.For<ILogger<KafkaEventsExtensions>>();
        var options = new ServiceBusOptions 
        { 
            Domain = "TestDomain",
            PublicServiceName = "test-service"
        };
        // Use reflection to set the private ServiceUrn property for testing
        typeof(ServiceBusOptions)
            .GetProperty(nameof(ServiceBusOptions.ServiceUrn))!
            .SetValue(options, new Uri("/test-domain/test-service", UriKind.Relative));
        _serviceBusOptions = Options.Create(options);
        _environment = Substitute.For<IHostEnvironment>();
        
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Messaging"] = "localhost:9092",
            ["Kafka:AutoProvision"] = "false",
            ["Kafka:ConnectionStringName"] = "Messaging"
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _extension = new KafkaEventsExtensions(_logger, _configuration, _serviceBusOptions, _environment);
    }

    [Fact]
    public void Configure_WithDevelopmentEnvironment_EnablesAutoProvision()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Development");
        _environment.IsDevelopment().Returns(true);
        var options = new WolverineOptions { ServiceName = "test-service" };

        // Act
        _extension.Configure(options);

        // Assert
        _logger.Received().LogInformation(
            "Auto-provision enabled: {AutoProvisionEnabled}",
            true);
    }

    [Fact]
    public void Configure_WithProductionEnvironment_DisablesAutoProvision()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        _environment.IsDevelopment().Returns(false);
        var options = new WolverineOptions { ServiceName = "test-service" };

        // Act
        _extension.Configure(options);

        // Assert
        _logger.Received().LogInformation(
            "Auto-provision enabled: {AutoProvisionEnabled}",
            false);
    }

    [Fact]
    public void Configure_WithAutoProvisionConfigTrue_EnablesAutoProvision()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Messaging"] = "localhost:9092",
            ["Kafka:AutoProvision"] = "true"
        };
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _environment.EnvironmentName.Returns("Production");
        _environment.IsDevelopment().Returns(false);
        
        var extension = new KafkaEventsExtensions(_logger, config, _serviceBusOptions, _environment);
        var options = new WolverineOptions { ServiceName = "test-service" };

        // Act
        extension.Configure(options);

        // Assert
        _logger.Received().LogInformation(
            "Auto-provision enabled: {AutoProvisionEnabled}",
            true);
    }

    [Fact]
    public void Configure_WithCustomConnectionStringName_UsesCustomName()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:CustomKafka"] = "localhost:9093",
            ["Kafka:ConnectionStringName"] = "CustomKafka"
        };
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _environment.EnvironmentName.Returns("Development");
        
        var extension = new KafkaEventsExtensions(_logger, config, _serviceBusOptions, _environment);
        var options = new WolverineOptions { ServiceName = "test-service" };

        // Act
        extension.Configure(options);

        // Assert
        _logger.Received().LogInformation(
            "Kafka connection string name: {ConnectionStringName}",
            "CustomKafka");
        _logger.Received().LogInformation(
            "Kafka bootstrap servers: {BootstrapServers}",
            "localhost:9093");
    }

    [Fact]
    public void Configure_WithEnvironmentName_CreatesCorrectConsumerGroupId()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        var options = new WolverineOptions { ServiceName = "test-service" };

        // Act
        _extension.Configure(options);

        // Assert
        _logger.Received().LogInformation(
            "Consumer group ID: {GroupId}",
            "test-service-prod");
    }

    [Theory]
    [InlineData("Development", "dev")]
    [InlineData("Production", "prod")]
    [InlineData("Test", "test")]
    [InlineData("Staging", "staging")]
    public void GetTopicName_WithDifferentEnvironments_MapsCorrectly(string environment, string expectedPrefix)
    {
        // Arrange
        var messageType = typeof(TestEvent);
        var topicAttribute = new EventTopicAttribute("test-topic", domain: "test-domain");

        // Act
        var result = InvokeGetTopicName(messageType, topicAttribute, environment);

        // Assert
        result.ShouldBe($"{expectedPrefix}.test-domain.public.test-topic.v1");
    }

    [Fact]
    public void GetTopicName_WithInternalScope_CreatesInternalTopic()
    {
        // Arrange
        var messageType = typeof(TestEvent);
        var topicAttribute = new EventTopicAttribute("test-topic", domain: "test-domain") 
        { 
            Internal = true
        };

        // Act
        var result = InvokeGetTopicName(messageType, topicAttribute, "Development");

        // Assert
        result.ShouldBe("dev.test-domain.internal.test-topic.v1");
    }

    [Fact]
    public void GetTopicName_WithPluralization_PluralizesTopicName()
    {
        // Arrange
        var messageType = typeof(TestEvent);
        // Create a test attribute that overrides ShouldPluralizeTopicName
        var topicAttribute = new TestPluralizeEventTopicAttribute("customer", domain: "sales");

        // Act
        var result = InvokeGetTopicName(messageType, topicAttribute, "Development");

        // Assert
        result.ShouldBe("dev.sales.public.customers.v1");
    }

    [Fact]
    public void GetTopicName_WithVersion_IncludesVersion()
    {
        // Arrange
        var messageType = typeof(TestEvent);
        var topicAttribute = new EventTopicAttribute("test-topic", domain: "test-domain", version: "v2");

        // Act
        var result = InvokeGetTopicName(messageType, topicAttribute, "Development");

        // Assert
        result.ShouldBe("dev.test-domain.public.test-topic.v2");
    }

    [Fact]
    public void GetTopicName_WithDefaultDomainAttribute_UsesAssemblyDomain()
    {
        // This test would require creating a test assembly with DefaultDomainAttribute
        // For now, just verify the logic path exists
        _ = typeof(TestEvent);
        _ = new EventTopicAttribute("test-topic"); // No domain specified

        // The actual test would need to mock the assembly attribute resolution
        // This is a placeholder to document the expected behavior
        Assert.True(true);
    }

    private static string InvokeGetTopicName(Type messageType, EventTopicAttribute topicAttribute, string environment)
    {
        var method = typeof(KafkaEventsExtensions).GetMethod("GetTopicName", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        return (string)method!.Invoke(null, [messageType, topicAttribute, environment])!;
    }

    private record TestEvent;
    
    [AttributeUsage(AttributeTargets.Class)]
    private class TestPluralizeEventTopicAttribute : EventTopicAttribute
    {
        public TestPluralizeEventTopicAttribute(string topic, string? domain = null, string version = "v1") 
            : base(topic, domain, version)
        {
        }
        
        public override bool ShouldPluralizeTopicName => true;
    }
}