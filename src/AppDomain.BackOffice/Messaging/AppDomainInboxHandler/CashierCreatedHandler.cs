// Copyright (c) OrgName. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using AppDomain.Cashiers.Contracts.IntegrationEvents;

namespace AppDomain.BackOffice.Messaging.AppDomainInboxHandler;

/// <summary>
///     Handles cashier created integration events for back office processing.
/// </summary>
[ExcludeFromCodeCoverage]
public static class CashierCreatedHandler
{
    /// <summary>
    ///     Processes a cashier created event by logging the cashier information.
    /// </summary>
    /// <param name="event">The cashier created integration event.</param>
    /// <param name="logger">Logger for tracking event processing.</param>
    /// <param name="_">Message bus for potential event publishing (unused).</param>
    /// <param name="__">Cancellation token for async operation (unused).</param>
    /// <returns>A completed task.</returns>
    public static Task Handle(CashierCreated @event, ILogger logger, IMessageBus _, CancellationToken __)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Cashier created event received for tenant {TenantId}, cashier {CashierId}",
                @event.TenantId, @event.Cashier.CashierId);
        }

        // TODO: Add business logic for processing cashier creation
        // Examples: Update read models, send notifications, sync with external systems

        return Task.CompletedTask;
    }
}
