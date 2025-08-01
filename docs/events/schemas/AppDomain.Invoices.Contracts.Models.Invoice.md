---
editLink: false
---

# Invoice

## Description

Represents a invoice entity.

## Schema

<!-- #region schema -->

| Property | Type | Required | Description |
| -------- | ---- | -------- | ----------- |
| TenantId| `Guid` | ✓| Gets or sets the tenantid. |
| InvoiceId| `Guid` | ✓| Gets or sets the invoiceid. |
| Name| `string` | ✓| Gets or sets the name. |
| Status| `string` | ✓| Gets or sets the status. |
| Amount| `decimal` | ✓| Gets or sets the amount. |
| Currency| `string` | | Gets or sets the currency. |
| DueDate| `Nullable<DateTime>` | | Gets or sets the duedate. |
| CashierId| `Nullable<Guid>` | | Gets or sets the cashierid. |
| AmountPaid| `Nullable<decimal>` | | Gets or sets the amountpaid. |
| PaymentDate| `Nullable<DateTime>` | | Gets or sets the paymentdate. |
| CreatedDateUtc| `DateTime` | ✓| Gets or sets the createddateutc. |
| UpdatedDateUtc| `DateTime` | ✓| Gets or sets the updateddateutc. |
| Version| `int` | ✓| Gets or sets the version. |


<!-- #endregion schema -->

