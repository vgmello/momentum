---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# PaymentStatusChanged

- **Status:** Active
- **Version:** v1
- **Entity:** `payment-status-changed`
- **Type:** Integration Event
- **Topic:** `{env}.testevents.public.payment-status-changeds.v1`
- **Estimated Payload Size:** 56 bytes ⚠️ *Contains dynamic properties*
- **Partition Keys**: TenantId
## Description

No documentation available

## Event Payload

| Property | Type | Required | Size | Description |
| ----------------------------------------------------------------- | --------- | -------- | -------- | --------------------------------------------------------------------- |
| TenantId| `Guid` | ✓| 16 bytes | No description available (partition key) |
| PaymentId| `string` | ✓| 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| PreviousStatus| `PaymentStatus` | ✓| 4 bytes | No description available |
| NewStatus| `PaymentStatus` | ✓| 4 bytes | No description available |
| ProcessedAt| `DateTime?` |  | 8 bytes | No description available |
| Notes| `string` |  | 0 bytes (Dynamic size - no MaxLength constraint) | No description available |
| Amount| `decimal?` |  | 16 bytes | No description available |
| PaymentMethod| `PaymentMethod` | ✓| 4 bytes | No description available |
| FailureReason| `FailureReason?` |  | 4 bytes | No description available |


### Partition Keys

This event uses a partition key for message routing:
- `TenantId` - No description available
    ## Technical Details

- **Full Type:** [TestEvents.AppDomain.Types.Contracts.IntegrationEvents.PaymentStatusChanged](#)
- **Namespace:** `TestEvents.AppDomain.Types.Contracts.IntegrationEvents`
- **Topic Attribute:** `[EventTopic]`
