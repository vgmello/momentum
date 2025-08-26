---
editLink: false
---

# ExternalPaymentGatewayResponseReceived

-   **Status:** Active
-   **Version:** v1
-   **Entity:** `external`
-   **Type:** Integration Event
-   **Topic:** `{env}.enterprise.public.external.v1`
-   **Partition Keys**: TenantId

## Description

Published when responses are received from external payment gateway systems

## When It's Triggered

This event is published when:

-   External payment gateway sends webhook responses
-   Asynchronous payment processing completes on third-party systems
-   Gateway status updates are received

## Namespace Structure

This event demonstrates deep namespace hierarchies:

-   Enterprise: Top-level organization namespace
-   AppDomain: Business domain
-   Payments: Sub-domain within AppDomain
-   Gateway: Specific service area
-   External: Implementation detail (external vs internal gateways)
-   Contracts: Contract definitions
-   IntegrationEvents: Event type classification

## Event Payload

| Property       | Type                         | Required | Description                                                                   |
| -------------- | ---------------------------- | -------- | ----------------------------------------------------------------------------- |
| TenantId       | `Guid`                       | ✓        | Identifier of the tenant for this payment gateway interaction (partition key) |
| GatewayName    | `string`                     | ✓        | Name of the external payment gateway (e.g., Stripe, PayPal, Square)           |
| TransactionId  | `string`                     | ✓        | Transaction identifier provided by the external gateway                       |
| ResponseStatus | `string`                     | ✓        | Status code or message returned by the gateway                                |
| ResponseData   | `Dictionary<string, Object>` | ✓        | Complete response payload from the gateway as key-value pairs                 |
| ReceivedAt     | `DateTime`                   | ✓        | Timestamp when the response was received from the gateway                     |

### Partition Keys

This event uses a partition key for message routing:

-   `TenantId` - Primary partition key based on tenant

## Technical Details

-   **Full Type:** [`Enterprise.AppDomain.Payments.Gateway.External.Contracts.IntegrationEvents.ExternalPaymentGatewayResponseReceived`](https://[github.url.from.config.com]/Enterprise/AppDomain/Payments/Gateway/External/Contracts/IntegrationEvents/ExternalPaymentGatewayResponseReceived.cs)
-   **Namespace:** `Enterprise.AppDomain.Payments.Gateway.External.Contracts.IntegrationEvents`
-   **Topic Attribute:** `[EventTopic<External>]`
