# Getting Started with AppDomain Solution

This guide will help you set up the AppDomain Solution development environment and make your first API calls to create cashiers and process invoices.

## Prerequisites

-   Docker or Docker compatible (Rancher, etc)
-   .NET 9 SDK with Aspire\* (Aspire is not required but recommended)
-   PostgreSQL client (optional, for database inspection)
-   API testing tool (Postman, curl, or similar)

## Option 1: .NET Aspire (Recommended)

The fastest way to get started is using .NET Aspire orchestration:

Make you have the aspire workload installed, you can install using the following command.

```bash
dotnet workload install aspire
```

```bash
# Run the entire AppDomain service stack
cd AppDomain/src/AppDomain.AppHost
dotnet run
```

This automatically:

-   ✅ Sets up PostgreSQL databases with Liquibase
-   ✅ Starts all services (API, BackOffice, Orleans)
-   ✅ Configures service discovery and dependencies
-   ✅ Provides observability dashboard
-   ✅ Uses persistent containers for consistent port allocation

## Option 2: Docker

For containerized deployment:

```bash
docker compose up --build
```

## Option 3: Manual Setup

For full control over the setup process:

### 1. Database Setup

Run these commands from the `AppDomain/infra/AppDomain.Database/` directory:

```bash
cd AppDomain/infra/AppDomain.Database/

# Step 1: Setup databases (creates actual dbs)
liquibase update --defaults-file liquibase.setup.properties

# Step 2: Service bus schema
liquibase update --defaults-file liquibase.servicebus.properties

# Step 3: Application schema
liquibase update
```

### 2. Run Individual Services

```bash
# Terminal 1 - API service
dotnet run --project AppDomain/src/AppDomain.Api

# Terminal 2 - Background service
dotnet run --project AppDomain/src/AppDomain.BackOffice

# Terminal 3 - Orleans service
dotnet run --project AppDomain/src/AppDomain.BackOffice.Orleans
```

### 3. Verify Setup

-   **API Health**: http://localhost:8101/health
-   **OpenAPI UI**: http://localhost:8101/scalar
-   **gRPC**: Connect to localhost:8102

## Making Your First API Calls

Once the system is running, let's create some AppDomain data:

### 1. Create a Cashier

First, let's create a cashier who will handle invoice payments:

```bash
curl -X POST http://localhost:8101/api/cashiers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Smith",
    "email": "john.smith@AppDomain.com"
  }'
```

Response:

```json
{
    "cashierId": "csh_123456",
    "name": "John Smith",
    "email": "john.smith@AppDomain.com",
    "createdAt": "2025-07-17T10:00:00Z"
}
```

### 2. Create an Invoice

Now let's create an invoice that our cashier can process:

```bash
curl -X POST http://localhost:8101/api/invoices \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Invoice for Services Q1",
    "amount": 1500.00,
    "currency": "USD",
    "dueDate": "2025-08-17",
    "cashierId": "csh_123456"
  }'
```

Response:

```json
{
    "invoiceId": "inv_789012",
    "name": "Invoice for Services Q1",
    "status": "Draft",
    "amount": 1500.0,
    "currency": "USD",
    "dueDate": "2025-08-17",
    "cashierId": "csh_123456",
    "createdAt": "2025-07-17T10:05:00Z"
}
```

### 3. Process a Payment

Simulate receiving a payment for the invoice:

```bash
curl -X POST http://localhost:8101/api/invoices/inv_789012/payment \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 1500.00,
    "paymentDate": "2025-07-17"
  }'
```

This triggers the event-driven workflow that:

1. Publishes a `PaymentReceived` event
2. Back office handler processes the payment
3. Invoice status updates to "Paid"
4. `InvoicePaid` event is published

### 4. Check Invoice Status

Verify the invoice has been marked as paid:

```bash
curl http://localhost:8101/api/invoices/inv_789012
```

## Exploring the System

### OpenAPI Documentation

Visit http://localhost:8101/scalar to explore all available endpoints:

-   **Cashiers API**: CRUD operations for cashier management
-   **Invoices API**: Invoice lifecycle management
-   **Bills API**: Coming soon

### Event Monitoring

The system publishes integration events for key operations:

-   `CashierCreated`, `CashierUpdated`, `CashierDeleted`
-   `InvoiceCreated`, `InvoiceCancelled`, `InvoicePaid`
-   `PaymentReceived`

### Database Inspection

Connect to PostgreSQL to explore the schema:

```bash
psql -h localhost -p 5432 -U postgres -d AppDomain
\dt AppDomain.*  # List all AppDomain tables
```

Key tables:

-   `AppDomain.cashiers` - Cashier records
-   `AppDomain.invoices` - Invoice records
-   `AppDomain.cashier_payments` - Payment history

## Next Steps

Now that you have the AppDomain Solution running:

1. **Explore the APIs**: Try different operations in the OpenAPI UI
2. **Review the Code**: Start with controllers in `src/AppDomain.Api/`
3. **Understand Events**: Check `src/AppDomain/*/Contracts/IntegrationEvents/`
4. **Run Tests**: Execute `dotnet test` to see the test suite
5. **Debug the System**: Set breakpoints in command handlers

For deeper dives:

-   [Cashiers Documentation](/guide/cashiers/)
-   [Invoices Documentation](/guide/invoices/)
-   [Architecture Overview](/arch/)
