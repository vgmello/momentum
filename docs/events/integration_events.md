<!-- prettier-ignore-start -->

### Invoice Events

| Event Name                                 | Description                                                               | Status |
| ------------------------------------------ | ------------------------------------------------------------------------- | ------ |
<!--#if (INCLUDE_SAMPLE) -->
| [InvoiceCreated](./AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceCreated.md)     | Published when a new invoice is created in the system                     | Active |
| [InvoiceCancelled](./AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceCancelled.md) | Published when an invoice is cancelled                                    | Active |
| [InvoiceFinalized](./AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceFinalized.md) | Published when an invoice is finalized during business day end processing | Active |
| [InvoicePaid](./AppDomain.Invoices.Contracts.IntegrationEvents.InvoicePaid.md)           | Published when an invoice is marked as paid                               | Active |
| [PaymentReceived](./AppDomain.Invoices.Contracts.IntegrationEvents.PaymentReceived.md)   | Published when a payment is received and processed                        | Active |

### Cashier Events

| Event Name                             | Description                                         | Status |
| -------------------------------------- | --------------------------------------------------- | ------ |
| [CashierCreated](./AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated.md) | Published when a new cashier is created             | Active |
| [CashierUpdated](./AppDomain.Cashiers.Contracts.IntegrationEvents.CashierUpdated.md) | Published when an existing cashier is updated       | Active |
| [CashierDeleted](./AppDomain.Cashiers.Contracts.IntegrationEvents.CashierDeleted.md) | Published when a cashier is deleted from the system | Active |
<!--#endif -->

<!-- prettier-ignore-end -->
