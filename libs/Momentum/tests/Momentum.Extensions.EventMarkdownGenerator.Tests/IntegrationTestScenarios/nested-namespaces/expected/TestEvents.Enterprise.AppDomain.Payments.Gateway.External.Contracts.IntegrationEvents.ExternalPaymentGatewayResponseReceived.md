---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# ExternalPaymentGatewayResponseReceived

- **Status:** Active
- **Version:** v1
- **Entity:** `external-payment-gateway-response-received`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.external-payment-gateway-response-receiveds.v1`
- **Estimated Payload Size:** 55 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| GatewayName| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| TransactionId| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| ResponseStatus| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| [ResponseData](/events/schemas/System.Object.md)| `Dictionary<string, Object>` | ✓| 31 bytes (Collection size estimated (no Range constraint)) | No description available |
| ReceivedAt| `DateTime` | ✓| 8 bytes | No description available |


### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available
    
### Reference Schemas

#### Objects

<!--@include: @/events/schemas/System.Object.md#schema-->

## Technical Details

- **Full Type:** [TestEvents.Enterprise.AppDomain.Payments.Gateway.External.Contracts.IntegrationEvents.ExternalPaymentGatewayResponseReceived](#)
- **Namespace:** `TestEvents.Enterprise.AppDomain.Payments.Gateway.External.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
