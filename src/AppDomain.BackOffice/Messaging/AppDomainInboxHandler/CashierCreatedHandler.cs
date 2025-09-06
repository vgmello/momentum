// Copyright (c) OrgName. All rights reserved.

using AppDomain.Cashiers.Contracts.IntegrationEvents;

namespace AppDomain.BackOffice.Messaging.AppDomainInboxHandler;

/// <summary>
///     Handles cashier created integration events for back office processing.
/// </summary>
public static class CashierCreatedHandler
{
    /// <summary>
    ///     Processes a cashier created event by logging the cashier information.
    /// </summary>
    /// <param name="event">The cashier created integration event.</param>
    /// <param name="logger">Logger for tracking event processing.</param>
    /// <param name="messaging">Message bus for potential event publishing.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A completed task.</returns>
    public static Task Handle(CashierCreated @event, ILogger logger, IMessageBus messaging, CancellationToken cancellationToken)
    {
        logger.LogDebug("Nice !!, It works. CashierCreated received by the backoffice {@Cashier}", @event.Cashier);

        return Task.CompletedTask;
    }
}
