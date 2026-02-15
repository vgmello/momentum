---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->
# Cashier

## Description

Represents a cashier entity.

## Schema

<!-- #region schema -->

| Property | Type | Required | Description |
| -------- | ---- | -------- | ----------- |
| TenantId| `Guid` | ✓| Gets or sets the tenantid. |
| CashierId| `Guid` | ✓| Gets or sets the cashierid. |
| Name| `string` | ✓| Gets or sets the name. |
| Email| `string` | ✓| Gets or sets the email. |
| [CashierPayments](/events/schemas/AppDomain.Cashiers.Contracts.Models.CashierPayment.md)| `IReadOnlyList<CashierPayment>` | ✓| Gets or sets the cashierpayments. |
| CreatedDateUtc| `DateTime` | ✓| Gets or sets the createddateutc. |
| UpdatedDateUtc| `DateTime` | ✓| Gets or sets the updateddateutc. |
| Version| `int` | ✓| Gets or sets the version. |


<!-- #endregion schema -->

### Reference Schemas

#### IReadOnlyCashierPayment
<!--@include: @/events/schemas/AppDomain.Cashiers.Contracts.Models.CashierPayment.md#schema-->

