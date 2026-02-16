// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Hosting;

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
            : DefaultDomainAttribute.GetDomainName(messageType.Assembly);

        var scope = topicAttribute.Internal ? "internal" : "public";

        var topicName = topicAttribute.ShouldPluralizeTopicName ? topicAttribute.Topic.Pluralize() : topicAttribute.Topic;

        var versionSuffix = string.IsNullOrWhiteSpace(topicAttribute.Version) ? null : $".{topicAttribute.Version}";

        return $"{_envName}.{domainName}.{scope}.{topicName}{versionSuffix}".ToLowerInvariant();
    }
}
