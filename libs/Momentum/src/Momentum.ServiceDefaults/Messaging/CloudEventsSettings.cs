// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.ServiceDefaults.Messaging;

public class CloudEventsSettings
{
    public EventFormat EventFormat { get; set; } = EventFormat.Json;
}

public enum EventFormat
{
    Avro,
    Json
}
