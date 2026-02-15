---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# Customer

## Description

Represents a customer entity.

## Schema

<!-- #region schema -->

| Property | Type | Required | Description |
| -------- | ---- | -------- | ----------- |
| CustomerId| `Guid` | ✓| Gets or sets the customerid. |
| Name| `string` | ✓| Gets or sets the name. |
| Email| `string` | ✓| Gets or sets the email. |
| [AppDomainAddress](/events/schemas/TestEvents.AppDomain.Orders.Contracts.Models.Address.md)| `Address` | ✓| Gets or sets the appdomainaddress. |


<!-- #endregion schema -->

### Reference Schemas

#### Address
<!--@include: @/events/schemas/TestEvents.AppDomain.Orders.Contracts.Models.Address.md#schema-->

