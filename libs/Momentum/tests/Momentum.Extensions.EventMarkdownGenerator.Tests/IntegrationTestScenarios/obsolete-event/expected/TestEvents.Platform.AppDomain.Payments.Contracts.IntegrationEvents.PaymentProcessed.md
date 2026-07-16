---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# PaymentProcessed

- **Status:** Active
- **Domain:** TestEvents
- **Subdomain:** Payments
- **Version:** v1
- **Entity:** `payment-processed`
- **Type:** Integration Event
- **Topic:** `payment-processeds`
- **Fully Qualified Topic:** `test-events.payments.payment-processeds.v1`
- **Estimated Payload Size:** 32 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| PaymentId| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| Amount| `decimal` | ✓| 16 bytes | No description available |


### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available
    ## Technical Details

- **Full Type:** [TestEvents.Platform.AppDomain.Payments.Contracts.IntegrationEvents.PaymentProcessed](#)
- **Type Name:** `PaymentProcessed`
- **Event Slug:** `payment-processed`
- **Namespace:** `TestEvents.Platform.AppDomain.Payments.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic<PaymentProcessed>]`
- **Attribute Properties:**
- `ShouldPluralizeTopicName`: `True`
- `Topic`: `payment-processed`
- `Domain`: *(empty)*
- `Subdomain`: *(empty)*
- `Version`: `v1`
- `Internal`: `False`
