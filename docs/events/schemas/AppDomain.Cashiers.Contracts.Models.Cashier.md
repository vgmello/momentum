---
editLink: false
---

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
| CashierPayments| `List<CashierPayment>` | ✓| Gets or sets the cashierpayments. |
| Version| `int` | ✓| Gets or sets the version. |


<!-- #endregion schema -->

