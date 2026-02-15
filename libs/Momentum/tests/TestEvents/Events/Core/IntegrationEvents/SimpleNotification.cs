// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace TestEvents.Events.Core.IntegrationEvents;

/// <summary>
///     A simple notification event with no partition keys.
/// </summary>
/// <param name="NotificationId">Unique notification identifier</param>
/// <param name="Message">The notification message</param>
[EventTopic<SimpleNotification>]
public sealed record SimpleNotification(
    Guid NotificationId,
    string Message
);
