<!-- prettier-ignore-start -->

### Invoice Events

| Event Name                                 | Description                                                               | Status |
| ------------------------------------------ | ------------------------------------------------------------------------- | ------ |
<!--#if (INCLUDE_SAMPLE)-->
| [InvoiceCreated](./invoice-created.md)     | Published when a new invoice is created in the system                     | Active |
| [InvoiceCancelled](./invoice-cancelled.md) | Published when an invoice is cancelled                                    | Active |
| [InvoiceFinalized](./invoice-finalized.md) | Published when an invoice is finalized during business day end processing | Active |
| [InvoicePaid](./invoice-paid.md)           | Published when an invoice is marked as paid                               | Active |
| [PaymentReceived](./payment-received.md)   | Published when a payment is received and processed                        | Active |

### Cashier Events

| Event Name                             | Description                                         | Status |
| -------------------------------------- | --------------------------------------------------- | ------ |
| [CashierCreated](./cashier-created.md) | Published when a new cashier is created             | Active |
| [CashierUpdated](./cashier-updated.md) | Published when an existing cashier is updated       | Active |
| [CashierDeleted](./cashier-deleted.md) | Published when a cashier is deleted from the system | Active |
<!--#endif -->

<!-- prettier-ignore-end -->
