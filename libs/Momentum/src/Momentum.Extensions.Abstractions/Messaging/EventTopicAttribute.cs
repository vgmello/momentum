// Copyright (c) ABCDEG. All rights reserved.

using Momentum.Extensions.Abstractions.Extensions;

namespace Momentum.Extensions.Abstractions.Messaging;

/// <summary>
///     Specifies the topic name for a distributed event class.
/// </summary>
/// <remarks>
///     Apply this attribute to distributed event classes to define their topic name
///     in the message broker. The topic name is used for routing messages to the
///     appropriate consumers. An optional domain can be specified to override the
///     assembly's default domain.
/// </remarks>
/// <example>
///     <code>
/// [EventTopic("user-created", domain: "users")]
/// public class UserCreatedEvent : IDistributedEvent { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public class EventTopicAttribute(string topic, string? domain = null, string version = "v1") : Attribute
{
    /// <summary>
    ///     Topic name for the event.
    /// </summary>
    public string Topic { get; } = topic;

    /// <summary>
    ///     Domain for the event.
    /// </summary>
    /// <value>
    ///     The domain name for this event, or <c>null</c> to use the assembly's default domain.
    /// </value>
    public string? Domain { get; } = domain;

    /// <summary>
    ///     Event major version (Default: 'v1')
    /// </summary>
    public string Version { get; } = version;

    /// <summary>
    ///     Indicates if the event is internal or public (Default: public)
    /// </summary>
    public bool Internal { get; set; } = false;


    public virtual bool ShouldPluralizeTopicName => false;
}

/// <summary>
///     Specifies the topic name for a distributed event class based on an entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type whose name will be used to generate the topic name.</typeparam>
/// <remarks>
///     This generic version automatically generates a topic name from the entity type name,
///     converting it to kebab-case. For example, <c>UserAccount</c> becomes <c>user-accounts</c>.
/// </remarks>
/// <example>
///     <code>
/// [EventTopic&lt;User&gt;(domain: "identity")]
/// public record UserCreatedEvent(Guid Id);
/// // Results in topic: "users"
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public class EventTopicAttribute<TEntity>(string? domain = null, string? topic = null, string version = "v1")
    : EventTopicAttribute(topic: topic ?? typeof(TEntity).Name.ToKebabCase(), domain: domain, version: version)
{
    public override bool ShouldPluralizeTopicName { get; } = string.IsNullOrEmpty(topic);
}
