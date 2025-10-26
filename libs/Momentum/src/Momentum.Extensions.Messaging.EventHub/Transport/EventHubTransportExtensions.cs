// Copyright (c) Momentum .NET. All rights reserved.

using Azure.Core;
using JasperFx.Core;
using Wolverine;
using Wolverine.Configuration;

namespace Momentum.Extensions.Messaging.EventHub.Transport;

/// <summary>
///     Extension methods for configuring Event Hub transport in Wolverine.
/// </summary>
public static class EventHubTransportExtensions
{
    /// <summary>
    ///     Configure Wolverine to use Azure Event Hubs with a fully qualified namespace and credential.
    /// </summary>
    public static EventHubTransport UseEventHub(
        this WolverineOptions opts,
        string fullyQualifiedNamespace,
        TokenCredential credential)
    {
        var transport = opts.AddOrFindTransport<EventHubTransport>();
        transport.FullyQualifiedNamespace = fullyQualifiedNamespace;
        transport.Credential = credential;
        return transport;
    }

    /// <summary>
    ///     Configure Wolverine to use Azure Event Hubs with a connection string.
    /// </summary>
    public static EventHubTransport UseEventHub(this WolverineOptions opts, string connectionString)
    {
        var transport = opts.AddOrFindTransport<EventHubTransport>();
        transport.ConnectionString = connectionString;
        return transport;
    }

    /// <summary>
    ///     Configure an existing Event Hub transport.
    /// </summary>
    public static EventHubTransport ConfigureEventHub(
        this WolverineOptions opts,
        Action<EventHubTransport> configure)
    {
        var transport = opts.AddOrFindTransport<EventHubTransport>();
        configure(transport);
        return transport;
    }

    /// <summary>
    ///     Enable auto-provisioning of Event Hubs (development environments only).
    /// </summary>
    public static EventHubTransport AutoProvision(this EventHubTransport transport)
    {
        transport.AutoProvisionEnabled = true;
        return transport;
    }

    /// <summary>
    ///     Configure a listener to an Event Hub.
    /// </summary>
    public static EventHubListenerConfiguration ListenToEventHub(
        this WolverineOptions opts,
        string eventHubName)
    {
        var transport = opts.AddOrFindTransport<EventHubTransport>();
        var endpoint = transport.EventHubs[eventHubName];
        endpoint.IsListener = true;

        return new EventHubListenerConfiguration(endpoint);
    }

    /// <summary>
    ///     Route messages to a specific Event Hub.
    /// </summary>
    public static ISubscriberConfiguration ToEventHub<T>(
        this IPublishToExpression publish,
        string eventHubName)
    {
        return publish.To<T>(EventHubEndpoint.BuildUri(eventHubName));
    }

    /// <summary>
    ///     Route messages to a specific Event Hub.
    /// </summary>
    public static ISubscriberConfiguration ToEventHub(
        this IPublishToExpression publish,
        string eventHubName)
    {
        return publish.To(EventHubEndpoint.BuildUri(eventHubName));
    }

    private static Uri BuildUri(string eventHubName)
    {
        return new Uri($"{EventHubTransport.ProtocolName}://{eventHubName}");
    }
}

/// <summary>
///     Configuration for Event Hub listeners.
/// </summary>
public class EventHubListenerConfiguration
{
    private readonly EventHubEndpoint _endpoint;

    internal EventHubListenerConfiguration(EventHubEndpoint endpoint)
    {
        _endpoint = endpoint;
    }

    /// <summary>
    ///     Configure the consumer group for this listener. Default is $Default.
    /// </summary>
    public EventHubListenerConfiguration ConsumerGroup(string consumerGroup)
    {
        _endpoint.ConsumerGroup = consumerGroup;
        return this;
    }

    /// <summary>
    ///     Configure processing to be inline (synchronous).
    /// </summary>
    public EventHubListenerConfiguration ProcessInline()
    {
        _endpoint.Mode = EndpointMode.Inline;
        return this;
    }

    /// <summary>
    ///     Configure processing to be buffered in memory (default, better throughput).
    /// </summary>
    public EventHubListenerConfiguration BufferedInMemory()
    {
        _endpoint.Mode = EndpointMode.BufferedInMemory;
        return this;
    }

    /// <summary>
    ///     Configure processing to be durable (persisted to database).
    /// </summary>
    public EventHubListenerConfiguration Durable()
    {
        _endpoint.Mode = EndpointMode.Durable;
        return this;
    }

    /// <summary>
    ///     Configure the number of partitions for auto-provisioning.
    /// </summary>
    public EventHubListenerConfiguration Partitions(int count)
    {
        _endpoint.PartitionCount = count;
        return this;
    }

    /// <summary>
    ///     Configure the message retention period for auto-provisioning.
    /// </summary>
    public EventHubListenerConfiguration Retention(TimeSpan retention)
    {
        _endpoint.MessageRetention = retention;
        return this;
    }
}
