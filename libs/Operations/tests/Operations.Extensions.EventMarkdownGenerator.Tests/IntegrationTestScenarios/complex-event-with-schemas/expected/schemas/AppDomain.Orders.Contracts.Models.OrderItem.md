# OrderItem

Represents an individual item within an order.

## Properties

| Property | Type | Required | Description |
| ----------------------------------------------------------------- | --------- | -------- | --------------------------------------------------------------------- |
| ProductId | `Guid` | ✓ | Unique identifier for the ordered product |
| ProductName | `string` | ✓ | Display name of the ordered product |
| Quantity | `int` | ✓ | Number of units ordered for this product |
| UnitPrice | `decimal` | ✓ | Price per unit of the product at time of order |
| TotalPrice | `decimal` | ✓ | Total price for this line item (quantity * unit price) |