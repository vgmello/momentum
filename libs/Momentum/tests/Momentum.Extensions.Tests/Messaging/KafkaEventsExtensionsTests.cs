// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.Messaging.Kafka;
using Momentum.ServiceDefaults.Messaging;
using NSubstitute;
using Wolverine;
using Wolverine.Kafka.Internals;

namespace Momentum.Extensions.Tests.Messaging;

public class KafkaEventsExtensionsTests
{
    private readonly ILogger<KafkaEventsExtensions> _logger = NullLogger<KafkaEventsExtensions>.Instance;
    private readonly IHostEnvironment _environment = Substitute.For<IHostEnvironment>();
    private readonly IOptions<ServiceBusOptions> _serviceBusOptions = Options.Create(new ServiceBusOptions());

    [Fact]
    public void Configure_WithAutoProvisionConfigTrue_EnablesAutoProvision()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Messaging"] = "localhost:9092",
                ["Kafka:AutoProvision"] = "true"
            })
            .Build();

        var extension = new KafkaEventsExtensions(_logger, config, _serviceBusOptions, _environment);
        var options = new WolverineOptions { ServiceName = "test-service" };
        var transport = options.Transports.GetOrCreate<KafkaTransport>();

        transport.AutoProvision = false;

        // Act
        extension.Configure(options);

        // Assert
        transport.AutoProvision.ShouldBeTrue();
    }

    [Fact]
    public void Configure_WithConnectionString_UsesConnectionString()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Messaging"] = "localhost:9093",
            })
            .Build();

        var extension = new KafkaEventsExtensions(_logger, config, _serviceBusOptions, _environment);
        var options = new WolverineOptions { ServiceName = "test-service" };
        var transport = options.Transports.GetOrCreate<KafkaTransport>();

        // Act
        extension.Configure(options);

        // Assert
        transport.ConsumerConfig.BootstrapServers.ShouldBe("localhost:9093");
        transport.ProducerConfig.BootstrapServers.ShouldBe("localhost:9093");
    }

    [Fact]
    public void Configure_WithEnvironmentName_CreatesCorrectConsumerGroupId()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Messaging"] = "localhost:909",
            })
            .Build();

        var extension = new KafkaEventsExtensions(_logger, config, _serviceBusOptions, _environment);
        var options = new WolverineOptions { ServiceName = "test-service" };
        var transport = options.Transports.GetOrCreate<KafkaTransport>();

        // Act
        extension.Configure(options);

        // Assert
        transport.ConsumerConfig.GroupId.ShouldBe("test-service-prod");
    }

    [Theory]
    [InlineData("Development", "dev")]
    [InlineData("Production", "prod")]
    [InlineData("Test", "test")]
    [InlineData("Staging", "stag")]
    public void GetTopicName_WithDifferentEnvironments_MapsCorrectly(string environment, string expectedPrefix)
    {
        // Arrange
        var messageType = typeof(TestEvent);
        var topicAttribute = new EventTopicAttribute("test-topic", domain: "test-domain");

        // Act
        var result = KafkaEventsExtensions.GetTopicName(environment, messageType, topicAttribute);

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
        var result = KafkaEventsExtensions.GetTopicName("Development", messageType, topicAttribute);

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
        var result = KafkaEventsExtensions.GetTopicName("Development", messageType, topicAttribute);

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
        var result = KafkaEventsExtensions.GetTopicName("Development", messageType, topicAttribute);

        // Assert
        result.ShouldBe("dev.test-domain.public.test-topic.v2");
    }

    private record TestEvent;

    [AttributeUsage(AttributeTargets.Class)]
    private class TestPluralizeEventTopicAttribute(string topic, string? domain = null, string version = "v1")
        : EventTopicAttribute(topic, domain, version)
    {
        public override bool ShouldPluralizeTopicName => true;
    }
}
