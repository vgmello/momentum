<!-- prettier-ignore-start -->

### Invoice Events

| Event Name                                 | Description                                                               | Status |
| ------------------------------------------ | ------------------------------------------------------------------------- | ------ |
# #if (INCLUDE_SAMPLE)
| [InvoiceCreated](./integration_events/AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceCreated.md)     | Published when a new invoice is created in the system                     | Active |
| [InvoiceCancelled](./integration_events/AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceCancelled.md) | Published when an invoice is cancelled                                    | Active |
| [InvoiceFinalized](./integration_events/AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceFinalized.md) | Published when an invoice is finalized during business day end processing | Active |
| [InvoicePaid](./integration_events/AppDomain.Invoices.Contracts.IntegrationEvents.InvoicePaid.md)           | Published when an invoice is marked as paid                               | Active |
| [PaymentReceived](./integration_events/AppDomain.Invoices.Contracts.IntegrationEvents.PaymentReceived.md)   | Published when a payment is received and processed                        | Active |

### Cashier Events

| Event Name                             | Description                                         | Status |
| -------------------------------------- | --------------------------------------------------- | ------ |
| [CashierCreated](./integration_events/AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated.md) | Published when a new cashier is created             | Active |
| [CashierUpdated](./integration_events/AppDomain.Cashiers.Contracts.IntegrationEvents.CashierUpdated.md) | Published when an existing cashier is updated       | Active |
| [CashierDeleted](./integration_events/AppDomain.Cashiers.Contracts.IntegrationEvents.CashierDeleted.md) | Published when a cashier is deleted from the system | Active |
# #endif

<!-- prettier-ignore-end -->
