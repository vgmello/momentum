// Copyright (c) Momentum .NET. All rights reserved.

using Azure.Messaging.EventHubs;
using Wolverine;

namespace Momentum.Extensions.Messaging.EventHub.Mapping;

/// <summary>
///     Maps Wolverine envelopes to/from Azure Event Hub EventData messages.
/// </summary>
public interface IEventHubEnvelopeMapper
{
    /// <summary>
    ///     Maps a Wolverine envelope to an outgoing EventData message.
    /// </summary>
    void MapEnvelopeToOutgoing(Envelope envelope, EventData outgoing);

    /// <summary>
    ///     Maps an incoming EventData message to a Wolverine envelope.
    /// </summary>
    void MapIncomingToEnvelope(Envelope envelope, EventData incoming);

    /// <summary>
    ///     Returns all header names that should be mapped.
    /// </summary>
    IEnumerable<string> AllHeaders();
}
