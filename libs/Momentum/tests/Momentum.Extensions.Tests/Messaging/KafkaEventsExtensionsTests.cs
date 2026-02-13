// Copyright (c) Momentum .NET. All rights reserved.

using Confluent.Kafka;
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

namespace Momentum.Extensions.Tests.Messaging;

public class KafkaSetupExtensionsTests
{
    private readonly IHostEnvironment _environment = Substitute.For<IHostEnvironment>();
    private readonly ILogger<KafkaWolverineExtensions> _logger = new NullLogger<KafkaWolverineExtensions>();
    private readonly IOptions<ServiceBusOptions> _serviceBusOptions = Options.Create(new ServiceBusOptions());
    private readonly ITopicNameGenerator _topicNameGenerator = Substitute.For<ITopicNameGenerator>();

    [Fact]
    public void KafkaWolverineExtensions_WithConnectionString_DoesNotThrow()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Messaging"] = "localhost:9092"
            })
            .Build();

        _environment.EnvironmentName.Returns("Development");
        var extension = new KafkaWolverineExtensions(_logger, config, _serviceBusOptions, _topicNameGenerator, "Messaging");
        var options = new WolverineOptions { ServiceName = "test-service" };

        // Act & Assert
        Should.NotThrow(() => extension.Configure(options));
    }

    [Fact]
    public void KafkaWolverineExtensions_WithoutConnectionString_Throws()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        _environment.EnvironmentName.Returns("Development");
        var extension = new KafkaWolverineExtensions(_logger, config, _serviceBusOptions, _topicNameGenerator, "Messaging");
        var options = new WolverineOptions { ServiceName = "test-service" };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => extension.Configure(options))
            .Message.ShouldContain("Kafka connection string 'Messaging' not set");
    }

    [Fact]
    public void KafkaAspireExtensions_AppliesProducerConfig()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:Confluent:Kafka:Producer:Messaging:Config:EnableIdempotence"] = "true",
                ["Aspire:Confluent:Kafka:Producer:Messaging:Config:MaxInFlight"] = "1",
                ["Aspire:Confluent:Kafka:Producer:Messaging:Config:Acks"] = "All",
                ["Aspire:Confluent:Kafka:Producer:Messaging:Config:MessageSendMaxRetries"] = "15"
            })
            .Build();

        var producerConfig = new ProducerConfig { BootstrapServers = "localhost:9092" };

        // Act
        KafkaAspireExtensions.ApplyAspireProducerConfig(config, "Messaging", producerConfig);

        // Assert
        producerConfig.EnableIdempotence.ShouldBe(true);
        producerConfig.MaxInFlight.ShouldBe(1);
        producerConfig.Acks.ShouldBe(Acks.All);
        producerConfig.MessageSendMaxRetries.ShouldBe(15);
    }

    [Fact]
    public void KafkaAspireExtensions_AppliesConsumerConfig()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:Confluent:Kafka:Consumer:Messaging:Config:SessionTimeoutMs"] = "15000",
                ["Aspire:Confluent:Kafka:Consumer:Messaging:Config:HeartbeatIntervalMs"] = "5000",
                ["Aspire:Confluent:Kafka:Consumer:Messaging:Config:MaxPollIntervalMs"] = "600000",
                ["Aspire:Confluent:Kafka:Consumer:Messaging:Config:FetchMinBytes"] = "2048",
                ["Aspire:Confluent:Kafka:Consumer:Messaging:Config:AutoOffsetReset"] = "Earliest"
            })
            .Build();

        var consumerConfig = new ConsumerConfig { BootstrapServers = "localhost:9092" };

        // Act
        KafkaAspireExtensions.ApplyAspireConsumerConfig(config, "Messaging", consumerConfig);

        // Assert
        consumerConfig.SessionTimeoutMs.ShouldBe(15000);
        consumerConfig.HeartbeatIntervalMs.ShouldBe(5000);
        consumerConfig.MaxPollIntervalMs.ShouldBe(600000);
        consumerConfig.FetchMinBytes.ShouldBe(2048);
        consumerConfig.AutoOffsetReset.ShouldBe(AutoOffsetReset.Earliest);
    }

    [Fact]
    public void KafkaAspireExtensions_AppliesSecurityConfig()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:Confluent:Kafka:Security:SecurityProtocol"] = "SaslSsl",
                ["Aspire:Confluent:Kafka:Security:SaslMechanism"] = "Plain",
                ["Aspire:Confluent:Kafka:Security:SaslUsername"] = "user123",
                ["Aspire:Confluent:Kafka:Security:SaslPassword"] = "pass456",
                ["Aspire:Confluent:Kafka:Security:SslCaLocation"] = "/path/to/ca.crt",
                ["Aspire:Confluent:Kafka:Security:SslCertificateLocation"] = "/path/to/cert.crt",
                ["Aspire:Confluent:Kafka:Security:SslKeyLocation"] = "/path/to/key.key"
            })
            .Build();

        var clientConfig = new ClientConfig { BootstrapServers = "localhost:9092" };

        // Act
        KafkaAspireExtensions.ApplyAspireClientConfig(config, "Messaging", clientConfig);

        // Assert
        clientConfig.SecurityProtocol.ShouldBe(SecurityProtocol.SaslSsl);
        clientConfig.SaslMechanism.ShouldBe(SaslMechanism.Plain);
        clientConfig.SaslUsername.ShouldBe("user123");
        clientConfig.SaslPassword.ShouldBe("pass456");
        clientConfig.SslCaLocation.ShouldBe("/path/to/ca.crt");
        clientConfig.SslCertificateLocation.ShouldBe("/path/to/cert.crt");
        clientConfig.SslKeyLocation.ShouldBe("/path/to/key.key");
    }


    [Theory]
    [InlineData("Development", "dev")]
    [InlineData("Production", "prod")]
    [InlineData("Test", "test")]
    [InlineData("Staging", "stag")]
    public void TopicNameGenerator_WithDifferentEnvironments_MapsCorrectly(string environment, string expectedPrefix)
    {
        // Arrange
        _environment.EnvironmentName.Returns(environment);
        var generator = new TopicNameGenerator(_environment);
        var messageType = typeof(TestEvent);
        var topicAttribute = new EventTopicAttribute("test-topic", domain: "test-domain");

        // Act
        var result = generator.GetTopicName(messageType, topicAttribute);

        // Assert
        result.ShouldBe($"{expectedPrefix}.test-domain.public.test-topic.v1");
    }

    [Fact]
    public void TopicNameGenerator_WithInternalScope_CreatesInternalTopic()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Development");
        var generator = new TopicNameGenerator(_environment);
        var messageType = typeof(TestEvent);
        var topicAttribute = new EventTopicAttribute("test-topic", domain: "test-domain")
        {
            Internal = true
        };

        // Act
        var result = generator.GetTopicName(messageType, topicAttribute);

        // Assert
        result.ShouldBe("dev.test-domain.internal.test-topic.v1");
    }

    [Fact]
    public void TopicNameGenerator_WithPluralization_PluralizesTopicName()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Development");
        var generator = new TopicNameGenerator(_environment);
        var messageType = typeof(TestEvent);
        var topicAttribute = new TestPluralizeEventTopicAttribute("customer", domain: "sales");

        // Act
        var result = generator.GetTopicName(messageType, topicAttribute);

        // Assert
        result.ShouldBe("dev.sales.public.customers.v1");
    }

    [Fact]
    public void TopicNameGenerator_WithVersion_IncludesVersion()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Development");
        var generator = new TopicNameGenerator(_environment);
        var messageType = typeof(TestEvent);
        var topicAttribute = new EventTopicAttribute("test-topic", domain: "test-domain", version: "v2");

        // Act
        var result = generator.GetTopicName(messageType, topicAttribute);

        // Assert
        result.ShouldBe("dev.test-domain.public.test-topic.v2");
    }

    [Fact]
    public void TopicNameGenerator_WithEmptyVersion_DoesNotIncludeVersion()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Development");
        var generator = new TopicNameGenerator(_environment);
        var messageType = typeof(TestEvent);
        var topicAttribute = new EventTopicAttribute("test-topic", domain: "test-domain", version: "");

        // Act
        var result = generator.GetTopicName(messageType, topicAttribute);

        // Assert
        result.ShouldBe("dev.test-domain.public.test-topic");
    }

    [Fact]
    public void TopicNameGenerator_WithNoDomain_FallsBackToAssemblyName()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Development");
        var generator = new TopicNameGenerator(_environment);
        var messageType = typeof(TestEvent); // Assembly: Momentum.Extensions.Tests → first segment: Momentum
        var topicAttribute = new EventTopicAttribute("test-topic"); // no domain

        // Act
        var result = generator.GetTopicName(messageType, topicAttribute);

        // Assert
        result.ShouldBe("dev.momentum.public.test-topic.v1");
    }

    private record TestEvent;

    [AttributeUsage(AttributeTargets.Class)]
    private class TestPluralizeEventTopicAttribute(string topic, string? domain = null, string version = "v1")
        : EventTopicAttribute(topic, domain, version)
    {
        public override bool ShouldPluralizeTopicName => true;
    }
}
