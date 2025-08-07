// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace Enterprise.AppDomain.Payments.Gateway.External.Contracts.IntegrationEvents;

/// <summary>
///     Published when responses are received from external payment gateway systems
/// </summary>
/// <param name="TenantId">Identifier of the tenant for this payment gateway interaction</param>
/// <param name="GatewayName">Name of the external payment gateway (e.g., Stripe, PayPal, Square)</param>
/// <param name="TransactionId">Transaction identifier provided by the external gateway</param>
/// <param name="ResponseStatus">Status code or message returned by the gateway</param>
/// <param name="ResponseData">Complete response payload from the gateway as key-value pairs</param>
/// <param name="ReceivedAt">Timestamp when the response was received from the gateway</param>
/// <remarks>
///     ## When It's Triggered
/// 
///     This event is published when:
///     - External payment gateway sends webhook responses
///     - Asynchronous payment processing completes on third-party systems
///     - Gateway status updates are received
/// 
///     ## Namespace Structure
/// 
///     This event demonstrates deep namespace hierarchies:
///     - Enterprise: Top-level organization namespace
///     - AppDomain: Business domain
///     - Payments: Sub-domain within AppDomain
///     - Gateway: Specific service area
///     - External: Implementation detail (external vs internal gateways)
///     - Contracts: Contract definitions
///     - IntegrationEvents: Event type classification
/// </remarks>
[EventTopic<ExternalPaymentGatewayResponseReceived>]
public sealed record ExternalPaymentGatewayResponseReceived(
    [PartitionKey(Order = 0)] Guid TenantId,
    string GatewayName,
    string TransactionId,
    string ResponseStatus,
    Dictionary<string, object> ResponseData,
    DateTime ReceivedAt
);
