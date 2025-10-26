// Copyright (c) Momentum .NET. All rights reserved.

using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Momentum.Extensions.Abstractions.Extensions;
using Momentum.Extensions.Messaging.EventHub.Mapping;
using Momentum.Extensions.Messaging.EventHub.Transport;
using Momentum.ServiceDefaults.Messaging;
using Wolverine;

namespace Momentum.Extensions.Messaging.EventHub.Configuration;

public static class EventHubSetupExtensions
{
    public const string SectionName = "Messaging";

    /// <summary>
    ///     Adds Event Hub messaging extensions with full Aspire integration to the WebApplicationBuilder.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder</param>
    /// <param name="serviceName">The connection name for Event Hub configuration</param>
    /// <param name="configureProducerOptions">Optional producer options configuration</param>
    /// <param name="configureProcessorOptions">Optional processor options configuration</param>
    /// <returns>The WebApplicationBuilder for chaining</returns>
    public static WebApplicationBuilder AddEventHubMessagingExtensions(
        this WebApplicationBuilder builder,
        string serviceName = SectionName,
        Action<EventHubProducerClientOptions>? configureProducerOptions = null,
        Action<EventProcessorClientOptions>? configureProcessorOptions = null)
    {
        var clientId = builder.Environment.ApplicationName.ToKebabCase();

        // Register topic/event hub name generator (reuse Kafka's implementation)
        builder.Services.AddSingleton<ITopicNameGenerator, TopicNameGenerator>();

        // Register CloudEvent mapper as the default envelope mapper
        builder.Services.AddSingleton<IEventHubEnvelopeMapper>(provider =>
            new CloudEventMapper(provider.GetRequiredService<IOptions<ServiceBusOptions>>()));

        // Register Wolverine extension for Event Hubs
        builder.Services.AddSingleton<IWolverineExtension>(provider =>
            new EventHubWolverineExtensions(
                provider.GetRequiredService<ILogger<EventHubWolverineExtensions>>(),
                provider.GetRequiredService<IConfiguration>(),
                provider.GetRequiredService<IOptions<ServiceBusOptions>>(),
                provider.GetRequiredService<ITopicNameGenerator>(),
                serviceName)
        );

        // Apply Aspire configuration
        EventHubAspireExtensions.ApplyAspireConfiguration(
            builder.Configuration,
            serviceName,
            clientId,
            builder.Environment.GetEnvNameShort());

        return builder;
    }
}

/// <summary>
///     Interface for topic/event hub name generation.
/// </summary>
public interface ITopicNameGenerator
{
    string GetTopicName(Type messageType, EventTopicAttribute topicAttribute);
}

/// <summary>
///     Generates Event Hub names following Momentum conventions.
/// </summary>
public class TopicNameGenerator(IHostEnvironment environment) : ITopicNameGenerator
{
    private readonly string _envName = environment.GetEnvNameShort();

    public string GetTopicName(Type messageType, EventTopicAttribute topicAttribute)
    {
        var domainName = !string.IsNullOrWhiteSpace(topicAttribute.Domain)
            ? topicAttribute.Domain
            : messageType.Assembly.GetAttribute<DefaultDomainAttribute>()!.Domain;

        var scope = topicAttribute.Internal ? "internal" : "public";

        var topicName = topicAttribute.ShouldPluralizeTopicName
            ? topicAttribute.Topic.Pluralize()
            : topicAttribute.Topic;

        var versionSuffix = string.IsNullOrWhiteSpace(topicAttribute.Version)
            ? null
            : $".{topicAttribute.Version}";

        return $"{_envName}.{domainName}.{scope}.{topicName}{versionSuffix}".ToLowerInvariant();
    }
}

// Import needed types from Kafka implementation
using JasperFx.Core.Reflection;
using Momentum.Extensions.Abstractions.Messaging;
