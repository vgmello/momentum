---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# PaymentProcessed

> [!CAUTION]
    > This event is deprecated. This event has been replaced by the new PaymentCompleted event. Please migrate to use the new event structure for enhanced functionality.

- **Status:** Deprecated
- **Version:** v1
- **Entity:** `payment-processed`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.payment-processeds.v1`
- **Estimated Payload Size:** 32 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId

## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| Amount| `decimal` | ✓| 16 bytes | No description available |
| TransactionId| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |


### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available
    ## Technical Details

- **Full Type:** [TestEvents.AppDomain.Legacy.Contracts.IntegrationEvents.PaymentProcessed](#)
- **Namespace:** `TestEvents.AppDomain.Legacy.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
