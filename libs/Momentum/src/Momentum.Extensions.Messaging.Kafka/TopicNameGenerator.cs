// Copyright (c) Momentum .NET. All rights reserved.

using JasperFx.Core.Reflection;
using Microsoft.Extensions.Hosting;
using Momentum.Extensions.Abstractions.Extensions;
using Momentum.Extensions.Abstractions.Messaging;

namespace Momentum.Extensions.Messaging.Kafka;

public interface ITopicNameGenerator
{
    /// <summary>
    ///     Generates a fully qualified topic name based on environment and domain.
    /// </summary>
    /// <param name="messageType">The integration event type.</param>
    /// <param name="topicAttribute">The event topic attribute.</param>
    /// <returns>A topic name in the format: {env}.{domain}.{scope}.{topic}.{version}</returns>
    string GetTopicName(Type messageType, EventTopicAttribute topicAttribute);
}

public class TopicNameGenerator(IHostEnvironment environment) : ITopicNameGenerator
{
    private readonly string _envName = environment.GetEnvNameShort();

    public string GetTopicName(Type messageType, EventTopicAttribute topicAttribute)
    {
        var domainName = !string.IsNullOrWhiteSpace(topicAttribute.Domain)
            ? topicAttribute.Domain
            : messageType.Assembly.GetAttribute<DefaultDomainAttribute>()!.Domain;

        var scope = topicAttribute.Internal ? "internal" : "public";

        var topicName = topicAttribute.ShouldPluralizeTopicName ? topicAttribute.Topic.Pluralize() : topicAttribute.Topic;

        var versionSuffix = string.IsNullOrWhiteSpace(topicAttribute.Version) ? null : $".{topicAttribute.Version}";

        return $"{_envName}.{domainName}.{scope}.{topicName}{versionSuffix}".ToLowerInvariant();
    }
}
